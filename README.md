# WeChatBot

用于微信自动回复消息的小工具!

直接通过 UIA 去识别聊天消息并进行处理,虽然很朴素但是非常安全(●ˇ∀ˇ●)

缺点也很显著,运行期间会模拟用户操作,你的其他工作会打断.所以推荐挂在一个没用的垃圾上跑~

## 提供指令

- `\help` 对各个指令的说明

![\\help_1](https://github.com/gz4nna/WeChatBot/blob/master/WeChatBot.Example/help_1.png?raw=true)

- `\bot` 模型对话功能:会将消息转发给模型,接收到模型的返回后输入并发送

![\\bot_1](https://github.com/gz4nna/WeChatBot/blob/master/WeChatBot.Example/bot_1.png?raw=true)

- `\picture` 发送随机图片,需要自备图库(目前未支持网络图库,使用的是文件夹)

![\\picture_1](https://github.com/gz4nna/WeChatBot/blob/master/WeChatBot.Example/picture_1.png?raw=true)

## 其他功能

### "拍一拍"自动响应

当出现"拍一拍"消息的时候,会识别到"拍一拍"触发的用户自定义文本,比如在`我拍了拍自己说你好`这句中,就可以将"说你好"这个内容读出来放在变量`patContent`中

处理在`WeChatBot.Console.Program.ProcessNewMessageAsync`方法里,可以自己修改回复的文本:

```csharp
await SendResponseToWeChatAsync($"[自动回复]绝对不许{patContent}");
```

### "撤回"自动响应

"撤回"的响应和上面的差不多,但是只回复了固定的文本

想要看撤回的内容,只需要单独维护一个UI树,在撤回发生时进行前后的比对拿到差异即可(~~这个我不敢搞~~)

## 如何使用

首先你需要正常登录微信,进入需要用到这个小机器人的群聊,然后运行这个程序即可😋

## 如何修改

### 模型对话功能

使用模型对话会调用`Commands/BotCommand.cs`文件中的`GetModelResponseAsync`方法,直接根据需要把`apiUrl`换成你的地址以及`requestData`改成你想要传的内容就好(~~现在就是直接变量写死,有朝一日会改成配置的~~)

```csharp
// 总之调用的地址在这里改
var apiUrl = "http://localhost:5000/v1/chat/completions";

var requestData = new Dictionary<string, object>
{
    { "messages", new[] { new { role = "user", content = prompt } } },
    { "mode", "instruct" },
    // 模板名放这里
    //{ "instruction_template", "" }, 
    // 角色放这里
    //{ "character", "" }, 
    //{ "temperature", 0.7 },
    //{ "top_p", 0.8 },
    //{ "top_k", 20 }
};
```

### 添加新指令

1. 首先在`WeChatBot.Console/Commands`下新建一个类,最好和其他几个文件一样写成`partial class Command`

2. 然后去`Program.Core.cs`文件中的`InitializeCommandHandlers`方法里添加新指令和触发的方法.如果前一步你的类是新加的,这里需要确实引用到

3. 最后,如果你的逻辑并不是单纯返回一个字符串,那么可以和我的`\picture`指令一样,通过添加特殊的标记,并在`SendResponseToWeChatAsync`中进行特判来执行你需要的其他动作

## 注意事项

- 需要有 .NET 8 运行时
- 运行期间请不要使用中文输入法,否则输出的英文内容将变成你输入法中的备选词
- 撤回响应功能目前不稳定,可能会有不可名状的错误导致整个死掉😭

## 其他

`\picture`演示时候用的猫是网图,侵删