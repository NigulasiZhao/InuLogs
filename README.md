# InuLogs

## 介绍

InuLogs 是一个专为 ASP.Net Core 网站应用程序和 API 设计的 HTTP 请求和异常记录器与查看器组件。它为开发人员提供了实时记录和查看网站应用程序中 HTTP 请求及其响应、以及运行时捕获的异常的功能。`InuLogs` 默认使用MongoDB 作为服务器端数据库，但也支持外部数据库，如 `Oracle` 、`PostgreSQL`、`MySQL` 和 `MSSQL`。此外，该组件还提供了在页面上重试 HTTP 请求的功能，并允许根据关键词检索响应内容、标记返回结果的准确性。这一强大的工具有助于开发人员更高效地进行调试和问题排查。

# ![Request & Response Viewer](https://github.com/NigulasiZhao/InuLogs/blob/main/inulog.png)

## 特征

- 实时 HTTP 请求、响应和异常记录器
- 代码内消息和事件记录
- 友好的记录器页面
- HTTP 和异常日志的搜索选项
- 记录器页面身份验证
- “自动清除日志”选项
- HTTP请求重试功能
- 根据关键词检索响应内容是否异常功能

## 支持
- .NET Core 3.1 及更高版本

## 安装

通过 .NET CLI 安装

```bash
dotnet add package InuLogs
```
通过包管理器安装

```bash
Install-Package InuLogs
```



## 用法
要使InuLogs能够监听请求，请使用InuLogs提供的InuLogs中间件。

在 `Startup.cs` 文件中添加InuLogs命名空间

```c#
using InuLogs;
```



### 在 `Startup.cs` `ConfigureService()`中注册InuLogs

```c#
services.AddInuLogServices();
```



### 设置清除日志选项 `Optional`
将在特定时间后清除日志.
>**注意**
> `IsAutoClear = true`
>当“默认计划时间”设置为“每周”时，请覆盖如下所示的设置:


```c#
services.AddInuLogServices(opt => 
{ 
   opt.IsAutoClear = true;
   opt.ClearTimeSchedule = InuLogsAutoClearScheduleEnum.Monthly;
});
```

### 设置外部数据库记录日志（MSSQL、MySQL、PostgreSQL、Oracle 和 MongoDb） `Optional`
添加数据库连接字符串并选择 DbDriver 选项

```c#
services.AddInuLogServices(opt => 
{
   opt.IsAutoClear = true; 
   opt.SetExternalDbConnString = "Server=localhost;Database=testDb;User Id=postgres;Password=root;"; 
   opt.DbDriverOption = InuLogsDbDriverEnum.PostgreSql; 
});
```



### 在HTTP请求管道中添加 InuLogs 中间件 `Startup.cs` `Configure()`
# ![Login page sample](https://github.com/NigulasiZhao/InuLogs/blob/main/login.png)

>**注意**
>添加身份验证选项，如下所示：

此身份验证信息（用户名和密码）将用于访问日志查页面。

```c#
app.UseInuLog(opt =>
{
    opt.InuPageUsername = "admin";
    opt.InuPagePassword = "123";
});
```


>**注意**
> 如果您的项目使用了身份验证，那么`app.UseInuLog()；` 应该在`app.UseRouting()`， `app.UseAuthentication()`， `app.UseAuthorization()`之后调用，按照这个顺序。

# ![Request and Response Sample Details](https://github.com/NigulasiZhao/InuLogs/blob/main/requestLog.png)

#### 可选配置: `Optional`
- 黑名单：要忽略的路由、路径或端点列表（应为逗号分隔的字符串，如下所示）。
- 序列化器：如果不是默认的，请指定使用的全局 json 序列化器/转换器的类型。
- CorsPolicy：如果项目使用 CORS，则为策略名称。

```c#
app.UseInuLog(opt => 
{ 
   opt.InuPageUsername = "admin"; 
   opt.InuPagePassword = "Qwerty@123"; 
   //Optional
   opt.Blacklist = "Test/testPost, api/auth/login"; //Prevent logging for specified endpoints
   opt.Serializer = InuLogsSerializerEnum.Newtonsoft; //If your project use a global json converter
   opt.CorsPolicy = "MyCorsPolicy";
 });
```

#### 添加 InuLogs 异常记录器 `Optional`
用于记录在特定 HTTP 请求期间发生的应用内异常。
# ![Exception Sample Details](https://github.com/NigulasiZhao/InuLogs/blob/main/exceptionLog.png)

>**注意**
>在UseInuLog中间件之前添加异常日志记录器，最好将其置于中间件层次结构的顶部，以便捕获可能的早期异常。

```c#
app.UseInuLogExceptionLogger();

...

app.UseInuLog(opt => 
{ 
   opt.InuPageUsername = "admin"; 
   opt.InuPagePassword = "Qwerty@123"; 
   ...
 });
```
### 记录消息/事件
```
InuLogger.Log("...Test Log...");
InuLogger.LogWarning(JsonConvert.Serialize(model));
InuLogger.LogError(res.Content, eventId: reference);
```
