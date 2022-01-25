## NBundles

基于Unity的AssetBundle的资源管理器
自动管理资源的引用计数和依赖关系，自动释放引用为0的bundle

## 编辑器模式
选择AssetBundles/Switch Mode/Editor Mode
编辑器模式，直接运行sample即可
直接按照资源路径加载指定的资源

## 模拟器模式
选择AssetBundles/Switch Mode/Simulate Mode

需要
1.先切换到Android或者iOS平台
2.打开PackageTool面板，填写app版本号
3.使用AssetBundleDispather工具，给自定义资源文件夹添加标记（可选，demo已标记）
4.点击Run All Checkers,给标记的文件自动添加BundleName
5.点击Build For Current Setting，Build出AssetBundle
6.点击Copy To StreamingAssets，拷贝Bundle资源到StreamingAssets目录下