using System;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace MyGameLib.NetCode.Tests
{
    public class NetworkSnapshotAckComponentTest
    {
        /// <summary>
        /// 1.客户端每隔2个tick收到快照
        /// 2.服务端收到客户端回应收到的tick和计算后的mask
        /// </summary>
        [Test]
        public void TestSnapshotMask()
        {
            NetworkSnapshotAckComponent clientAck = new NetworkSnapshotAckComponent();
            NetworkSnapshotAckComponent serverAck = new NetworkSnapshotAckComponent();

            // 1.客户端收到服务端快照 tick = 2
            // 由于第一次收到，mask = mask | 1;
            clientAck.UpdateLocalValues(2U);
            LogClientInfo(clientAck);
            Assert.AreEqual(clientAck.LastReceivedSnapshotByLocal, 2);

            // 2.回给服务端
            serverAck.UpdateReceiveByRemote(clientAck.LastReceivedSnapshotByLocal,
                clientAck.ReceivedSnapshotByLocalMask);
            LogServerInfo(serverAck);
            Assert.AreEqual(serverAck.LastReceivedSnapshotByRemote, 2);
            Assert.AreEqual(serverAck.ReceivedSnapshotByRemoteMask0, 1);

            // 3.客户端第二次收到快照 tick = 4
            clientAck.UpdateLocalValues(4U);
            LogClientInfo(clientAck);
            Assert.AreEqual(clientAck.LastReceivedSnapshotByLocal, 4);
        }

        [Test]
        public void TestSnapshotMask1()
        {
            NetworkSnapshotAckComponent clientAck = new NetworkSnapshotAckComponent();
            NetworkSnapshotAckComponent serverAck = new NetworkSnapshotAckComponent();

            for (uint i = 1; i <= 130; i++)
            {
                if (i % 2 == 0)
                {
                    // 1.客户端收到服务端快照 tick = 2
                    // 由于第一次收到，mask = mask | 1;
                    clientAck.UpdateLocalValues(i);

                    // 2.回给服务端
                    serverAck.UpdateReceiveByRemote(clientAck.LastReceivedSnapshotByLocal,
                        clientAck.ReceivedSnapshotByLocalMask);
                }
            }
            
            LogClientInfo(clientAck);
            LogServerInfo(serverAck);

        }

        private void LogServerInfo(NetworkSnapshotAckComponent ack)
        {
            Debug.Log("");
            Debug.Log("============== LogServerInfo =================");
            Debug.Log($"Remote:{ack.LastReceivedSnapshotByRemote}");
            Debug.Log($"Mask3 - {ToString(ack.ReceivedSnapshotByRemoteMask3)}");
            Debug.Log($"Mask2 - {ToString(ack.ReceivedSnapshotByRemoteMask2)}");
            Debug.Log($"Mask1 - {ToString(ack.ReceivedSnapshotByRemoteMask1)}");
            Debug.Log($"Mask0 - {ToString(ack.ReceivedSnapshotByRemoteMask0)}");
            Debug.Log("===============================================");
        }

        private void LogClientInfo(NetworkSnapshotAckComponent ack)
        {
            Debug.Log("");
            Debug.Log("============== LogClientInfo =================");
            Debug.Log($"Local:{ack.LastReceivedSnapshotByLocal}");
            Log(ack.ReceivedSnapshotByLocalMask);
            Debug.Log("===============================================");
        }

        private string ToString(ulong val)
        {
            byte[] bytes = BitConverter.GetBytes(val);
            string s = "";
            foreach (byte b in bytes)
            {
                s = Convert.ToString(b, 2).PadLeft(8, '0') + " " + s;
            }

            return s;
        }

        private void Log(uint val)
        {
            Debug.Log(Convert.ToString(val, 2));
        }
    }
}