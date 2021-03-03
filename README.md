# NetCode-FPS
[Bilibli 车辆同步演示视频](https://www.bilibili.com/video/BV1TA411W7zt/)  
[Bilibli FPS同步演示视频(审核中)](https://www.bilibili.com/video/BV1ih411k7bS/)

**特点**:
* 客户端玩家预测
* 客户端远程玩家插值
* 回滚和重播
* 实时调整客户端对于服务端Tick的预测，以优化服务端的输入Buffer

**参考**:
* [Client-Side Prediction With Physics in Unity](http://www.codersblock.org/blog/client-side-prediction-in-unity-2018)
* [Gaffer On Games Networking](https://gafferongames.com/tags/networking/)
* [Gabriel Gambetta Fast-Paced Multiplayer](https://www.gabrielgambetta.com/client-side-prediction-server-reconciliation.html)
* [Overwatch Gameplay Architecture & Netcode](https://www.youtube.com/watch?v=W3aieHjyNvw)
* https://www.gamedev.net/forums/topic/696756-command-frames-and-tick-synchronization/
* Unity NetCode

## 运行Demo

### 说明
Unity版本:2020.1.17f1c1  
整个工程全部用DOTS以及混合GameObject开发。

>注：请使用同版本UnityEditor打开工程
### 两个示例场景:
1. Scene/NetCube
2. Scene/NetFPS

打开场景Editor下直接Play即可

### NetCube示例的操作
1. `WASD`移动
2. `Space`向上施加力
3. `鼠标左键`重置服务端Cube位置，以测试客户端与服务端位置不一致时的拉扯的效果.

### NetFPS示例的操作
1. `WASD`移动
2. `Space`跳跃
3. `鼠标左键`开火
4. `Left Shift`加速
5. `R`手雷
6. `T`重置服务端Playe位置，以测试客户端与服务端位置不一致时的拉扯的效果.
7. `F12`锁定鼠标
8. `Esc`释放锁定的鼠标
9. 玩家被击中时，当血量等于0时，在服务端会把玩家的位置重置。

### Build

Assets下有两个Build配置：  
1. `WindowsClassicBuildConfiguration` 构建客户端
2. `WindowsClassicBuildConfigurationSev` 构建服务端

构建完成后即可在`NetCode-FPS/Build`中找到`Ser服务端`以及`Test客户端`
