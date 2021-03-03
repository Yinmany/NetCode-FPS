using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport.Utilities;
using UnityEngine;


namespace MyGameLib.NetCode
{
    /**
     * commandAge在服务器上计算并发送到客户端。
     * 它跟踪接收到的命令与服务器上正在处理的当前滴答进行比较的时间。
     * 如果命令平均到达服务器上需要它们之前的两个滴答，那么commandAge将为-2。
     * 客户端使用从服务器接收到的最新commandAge来调整增量时间，并使commandAge更加接近目标。
     * 因此，例如，如果commandAge是-2.5，则命令到达的时间要比服务器上的到达时间早一些，则客户端将以较低的速度运行一段时间，因此它不会比server提前那么远，commandAge会更接近-2。
     * 因为commandAge是在服务器上计算并发送到客户端的，所以我们必须等待一个完整的RTT进行调整，直到在客户端上看到对commandAge的任何更新。
     * 为了弥补这一点，我们跟踪了在一个完整的RTT（即commandAgeAdjustment循环缓冲区）期间尝试调整commandAge的量的近似值。
     * 因此，如果commandAge为-2.5，但是我们已经在最后一个RTT * framRate刻度上进行了+0.5刻度的累积时间调整，那么我们现在假设它实际上是-2，但尚未从服务器接收到更新的值。
     * 我们处理抖动的唯一方法是将kTargetCommandSlack设置为2。这意味着我们试图确保命令平均到达服务器之前需要2个滴答。
     * 只要您的抖动小于2个滴答声，在60Hz模拟下约为33ms，就可以了。
     * 应该有可能使kTargetCommandSlack动态化，并根据我们作为RTT计算的一部分计算出的抖动来使其更好地处理真正的高抖动，但这不是我们现在打算进行的工作。
     */
    [UpdateInWorld(TargetWorld.Client)]
    [UpdateInGroup(typeof(NetworkReceiveSystemGroup))]
    [UpdateBefore(typeof(NetworkStreamReceiveSystem))]
    public class NetworkTimeSystem : ComponentSystem
    {
        public static uint TimestampMS =>
            (uint) (System.Diagnostics.Stopwatch.GetTimestamp() / System.TimeSpan.TicksPerMillisecond);

        private const uint kTargetCommandSlack = 2;
        private const int KInterpolationTimeNetTicks = 2;
        private const int KInterpolationTimeMS = 0;

        private uint latestSnapshotEstimate { get; set; }

        public uint predictTargetTick { get; set; }
        public float subPredictTargetTick { get; set; }

        private EntityQuery connectionGroup;

        /// <summary>
        /// 记录一个rtt前调整的值
        /// </summary>
        private NativeArray<float> commandAgeAdjustment;

        private int commandAgeAdjustmentSlot;


        public float currentInterpolationFrames { get; set; }
        public uint interpolateTargetTick { get; set; }
        public float subInterpolateTargetTick { get; set; }

        private RenderTime _renderTime = new RenderTime();

        protected override void OnCreate()
        {
            connectionGroup = GetEntityQuery(ComponentType.ReadOnly<NetworkSnapshotAckComponent>());
            RequireSingletonForUpdate<ClientServerTickRate>();
            RequireSingletonForUpdate<NetworkSnapshotAckComponent>();
            latestSnapshotEstimate = 0;
            commandAgeAdjustment = new NativeArray<float>(64, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            commandAgeAdjustment.Dispose();
        }

        protected override void OnUpdate()
        {
            if (connectionGroup.IsEmptyIgnoreFilter)
            {
                predictTargetTick = 0;
                latestSnapshotEstimate = 0;
                return;
            }

            var tickRate = default(ClientServerTickRate);
            if (HasSingleton<ClientServerTickRate>())
                tickRate = GetSingleton<ClientServerTickRate>();
            tickRate.ResolveDefaults();
            var ack = GetSingleton<NetworkSnapshotAckComponent>();

            // FIXME: 根据延迟进行调整
            uint interpolationTimeMS = KInterpolationTimeMS;
            if (interpolationTimeMS == 0)
            {
                // 2000 + 19 / 20 = 100.95ms = 100ms
                interpolationTimeMS = (1000 * KInterpolationTimeMS + (uint) tickRate.NetworkTickRate - 1) /
                                      (uint) tickRate.NetworkTickRate;
            }

            // 估计往返时间
            float estimatedRTT = ack.EstimatedRTT;

            // 固定+2.5f
            float interpolationFrames = 0.5f + kTargetCommandSlack +
                                        ((estimatedRTT + 4 * ack.DeviationRTT + interpolationTimeMS) / 1000f) *
                                        tickRate.SimulationTickRate;


            if (latestSnapshotEstimate == 0)
            {
                if (ack.LastReceivedSnapshotByLocal == 0)
                {
                    predictTargetTick = 0;
                    return;
                }

                latestSnapshotEstimate = ack.LastReceivedSnapshotByLocal;

                // 估算出服务端tick
                predictTargetTick = GetCurrentPredictTick(estimatedRTT, tickRate.SimulationTickRate);
                currentInterpolationFrames = interpolationFrames;
                for (int i = 0; i < commandAgeAdjustment.Length; ++i)
                    commandAgeAdjustment[i] = 0;
            }
            else
            {
                latestSnapshotEstimate = ack.LastReceivedSnapshotByLocal;
            }


            int curSlot = (int) (ack.LastReceivedSnapshotByLocal % commandAgeAdjustment.Length);

            // 当来一个新的数据时，清除前面的数据.
            if (curSlot != commandAgeAdjustmentSlot)
            {
                for (int i = (commandAgeAdjustmentSlot + 1) % commandAgeAdjustment.Length;
                    i != (curSlot + 1) % commandAgeAdjustment.Length;
                    i = (i + 1) % commandAgeAdjustment.Length)
                {
                    commandAgeAdjustment[i] = 0;
                }

                commandAgeAdjustmentSlot = curSlot;
            }

            // 服务器输入buffer的数量 负数:没被消耗的数量 正数:服务端不够的数量
            float commandAge = ack.ServerCommandAge / 256f + kTargetCommandSlack;

            // 估算rtt有多少个tick
            int rttInTicks = (int) ((uint) estimatedRTT * (uint) tickRate.SimulationTickRate / 1000);
            if (rttInTicks > commandAgeAdjustment.Length)
                rttInTicks = commandAgeAdjustment.Length;

            // 因为commandAge是在服务器上计算并发送到客户端的，所以我们必须等待一个完整的RTT进行调整，直到在客户端上看到对commandAge的任何更新。
            // 为了弥补这一点，我们跟踪了在一个完整的RTT（即commandAgeAdjustment循环缓冲区）期间尝试调整commandAge的量的近似值。

            // 记录了在一个完整RTT中客户端自己调整commandAge的值
            // 每次Update时为了从最后一次收到的commandAge(每次计算都是从最后一次收到服务端的age计算的)值中,还原客户端自己最新调整的值(为了接着上次自己调整的值继续调整)。
            for (int i = 0; i < rttInTicks; ++i)
                commandAge -=
                    commandAgeAdjustment[
                        (commandAgeAdjustment.Length + commandAgeAdjustmentSlot - i) % commandAgeAdjustment.Length];

            // 有多少个tick  1s/50tick
            float deltaTicks = Time.DeltaTime * tickRate.SimulationTickRate;

            float predictionTimeScale = 1.0f;
            
            // 小于10个tick,就进行微调。
            if (math.abs(commandAge) < 10)
            {
                predictionTimeScale = math.clamp(1.0f + 0.1f * commandAge, 0.9f, 1.1f);
                subPredictTargetTick += deltaTicks * predictionTimeScale;
                uint pdiff = (uint) subPredictTargetTick;
                subPredictTargetTick -= pdiff;
                predictTargetTick += pdiff;
            }
            else // 大于10个刻度，重新预测.直接追上最新预测.
            {
                uint curPredict = GetCurrentPredictTick(estimatedRTT, tickRate.SimulationTickRate);

                for (int i = 0; i < commandAgeAdjustment.Length; ++i)
                    commandAgeAdjustment[i] = 0;

                subPredictTargetTick = 0;
                predictTargetTick = curPredict;
                return;
            }

            // 记录客户端自己对commandAge进行调整的值(在一个完整RTT期间中，客户端都是自己在尝试对commandAge进行调整.)
            // 以便在下一个commandAge没到之前，自行对commandAge尝试调整(每次Update调用都会进行调整,所有记录了一个完整RTT期间尝试的调整值).
            commandAgeAdjustment[commandAgeAdjustmentSlot] += deltaTicks * (predictionTimeScale - 1.0f);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            NetDebug.Set(nameof(commandAge), commandAge);
            NetDebug.Set(nameof(predictionTimeScale), predictionTimeScale);
            NetDebug.Set(nameof(predictTargetTick), predictTargetTick);
#endif

            // currentInterpolationFrames +=
            //     math.clamp((interpolationFrames - currentInterpolationFrames * 0.1f), -0.1f, 0.1f);
            //
            // var idiff = (uint) currentInterpolationFrames;
            // interpolateTargetTick = predictTargetTick - idiff;
            // var subidiff = currentInterpolationFrames - idiff;
            // subidiff -= subInterpolateTargetTick + subPredictTargetTick;
            // if (subidiff < 0)
            // {
            //     ++interpolateTargetTick;
            //     subidiff = -subidiff;
            // }
            // else if (subidiff > 0)
            // {
            //     idiff = (uint) subidiff;
            //     subidiff -= idiff;
            //     interpolateTargetTick -= idiff;
            //     subidiff = 1f - subidiff;
            // }
            //
            // subInterpolateTargetTick = subidiff;


            _renderTime.TickRate = tickRate;
            _renderTime.UpdateTime(ack.LastReceivedSnapshotByLocal, Time.DeltaTime);
            interpolateTargetTick = _renderTime.InterpolateTargetTick;
            subInterpolateTargetTick = _renderTime.InterpolateFaction;

            NetDebug.RenderTick = interpolateTargetTick;
            NetDebug.Set("lastRecvSnapDiff",
                $"{_renderTime.Diff}| Error:{_renderTime.Error} Min:{_renderTime.MinError} Max:{_renderTime.MaxError}");
        }

        /// <summary>
        /// FIXME: adjust by latency
        /// </summary>
        public struct RenderTime
        {
            private uint previousServerTick;

            private float faction;

            public uint Diff { get; private set; }
            public uint InterpolateTargetTick { get; private set; }
            public float InterpolateFaction { get; private set; }

            public ClientServerTickRate TickRate;

            public float Error { get; private set; }
            private float prevousError;

            public float MaxError { get; private set; }
            public float MinError { get; private set; }

            public const float Kp = 0.05f;
            public const float Kd = 0.01f;

            public void AddDuration(float duration)
            {
                float interval = 1f / TickRate.SimulationTickRate;
                float netInterval = 1f / TickRate.NetworkTickRate;

                faction += duration;
                InterpolateTargetTick += (uint) math.clamp(faction / interval, 0, 1);
                faction %= interval;
                InterpolateFaction = faction / interval;
            }

            public void SetTime(uint tick, float faction)
            {
                InterpolateTargetTick = tick;
                this.faction = faction;
            }

            public void UpdateTime(uint lastServerTick, float dt)
            {
                if (lastServerTick > previousServerTick)
                {
                    Diff = lastServerTick - previousServerTick;
                    previousServerTick = lastServerTick;
                }

                AddDuration(dt);

                if (InterpolateTargetTick >= lastServerTick || InterpolateTargetTick == 0)
                {
                    SetTime(lastServerTick, 0f);
                }

                prevousError = Error;

                // 误差(差几个快照?)
                Error = (lastServerTick - InterpolateTargetTick) / (float) Diff;

                if (Error > MaxError)
                {
                    MaxError = Error;
                    if (MinError == 0) MinError = MaxError;
                }

                if (Error < MinError)
                {
                    MinError = Error;
                }

                // 相差太大，强制设置。
                if (Error > 10)
                {
                    SetTime(lastServerTick - Diff * 8, 0f);
                    // Debug.LogError($"插值Tick调整{InterpolateTargetTick} {lastServerTick} {Diff} ++");
                }

                // 变速
                if (Error > 1)
                {
                    // 116ms

                    float acc = (Kp * Error) + (Kd * ((Error - prevousError) / dt)) * dt;
                    AddDuration(acc);
                    // Debug.LogError($"插值Tick调整{InterpolateTargetTick} {lastServerTick} {Diff} --");
                }
            }
        }

        /// <summary>
        /// 获取当前最新预测的tick
        /// 如：A(estimatedRTT) = 100, B(simTickRate) = 50
        /// A/(1000/B) = A*B/1000
        /// 最后 + 算RTT(ms)有多少个Tick并加上0.999固定偏移
        /// </summary>
        /// <param name="estimatedRTT">往返延迟</param>
        /// <param name="simTickRate">逻辑模拟帧数</param>
        /// <returns></returns>
        private uint GetCurrentPredictTick(float estimatedRTT, int simTickRate)
        {
            return latestSnapshotEstimate + kTargetCommandSlack +
                   ((uint) estimatedRTT * (uint) simTickRate + 999) / 1000;
        }
    }
}