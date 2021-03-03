# NetCode-FPS
多人FPS演示，该演示集成了许多现代网络代码技术以提高游戏质量。

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

### Build

Assets下有两个Build配置：  
1. `WindowsClassicBuildConfiguration` 构建客户端
2. `WindowsClassicBuildConfigurationSev` 构建服务端

构建完成后即可在`NetCode-FPS/Build`中找到`Ser服务端`以及`Test客户端`