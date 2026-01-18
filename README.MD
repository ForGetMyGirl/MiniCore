# MiniCore v1.0

## 框架结构说明

MIniCore大部分功能都写成了Component的形式，通过Global.Com.Add<'AComponent>和Global.Com.Get<'AComponent>的形式可以添加或者获取组件；
目前实现的组件有：CSV表读取、文件操作、UI窗体（MVP的形式）、网络组件（Tcp、Kcp、Udp）等；
整个框架分为3个模块，分别是MiniCore.Model、MiniCore.Core和MiniCore.HotUpdate;

- MiniCore.Model：主要存储一些基础的实体、接口等定义；
- MiniCore.Core：主要是一些具体的组件的实现、工具类等；
- MiniCore.HotUpdate：用于热更新的程序集，可以通过HybridCLR等进行热更新；

## 框架简单使用说明：
1. UI资源加载通过MVP的方式，可以通过Unity上方菜单栏 MiniCore ==》UI Window Generator输入窗体名称通过自定义模板（可自由更改）一键生成View和Presenter，并且通过UIFactoryComponent直接加载窗体；
2. 网络组件目前支持Tcp\Kcp\Udp，并且可以自定义序列化，可以灵活的使用Json、Protocol等。
3. 写好网络请求体和响应体（entity）及其相关的Handler以后，可以点解MiniCore==>Opcode==>Generate(HotUpdate)自动生成opcode与网络消息类型的绑定，注意：自动生成代码只会在名字中含有HotUpdate的程序集中查找，建议直接卸载MiniCore.HotUpdate程序集中，可以直接无缝衔接HybridCLR做代码热更新；
4. Kcp目前是通过AI将[Kcp C#版](https://github.com/KumoKyaku/kcp)引入的方式生成的，由于只审查了接入的Session层面，因此可能存在潜在的bug还没及时修复，如有问题，可以联系我，看到后我会及时修复。
5. 目前使用YooAsset作为资源加载方案，使用前请注意，如需使用HybridCLR，需要自己去官网下载安装，可以无缝集成到本系统中。

## 引用的插件

1. 官方插件：

- [Newtonsoft Json]：目前主要用于默认网络报文序列化

2. 三方插件：

- [UniTask](https://github.com/Cysharp/UniTask?tab=readme-ov-file#upm-package)

## 总结：

整个框架使用简单，便捷，可以快速上手，由于个人经验以及时间的原因，可能存在一些问题，欢迎各位大佬指正。

---