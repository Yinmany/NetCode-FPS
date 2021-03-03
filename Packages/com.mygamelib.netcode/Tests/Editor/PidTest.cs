using NUnit.Framework;
using UnityEngine;

namespace MyGameLib.NetCode.Tests
{
    public class PidTest
    {
        /// <summary>
        /// 根据误差来加速减数
        /// 就是时间增量 与 tick误差的关系
        /// tick误差就是：收到的服务端最新帧数与当前渲染帧数的差
        /// </summary>
        [Test]
        public void P()
        {
            int lastTick = 3;
            int curTick = 0;

            int error = lastTick - curTick;

            float time = 0f;
            for (int i = 0; i < 10; i++)
            {
                time += 0.016f;
                time += error * 0.01f;
                curTick += (int) (time / 0.02f);
                time %= 0.02f;
                error = lastTick - curTick;
                Debug.Log($"i={i} curTick={curTick} error={error} time={time}");
            }
        }

        [Test]
        public void Pid()
        {
            // 上次偏差
            float previous_error = 0;
            float interfral = 0;
        }
    }
}