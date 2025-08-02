# WeChatBot

用于微信自动回复消息的小工具!

直接通过 UIA 去识别聊天消息并进行处理,虽然很朴素但是非常安全(●ˇ∀ˇ●)

缺点也很显著,运行期间会模拟用户操作,你的其他工作会打断.所以推荐挂在一个没用的垃圾上跑~

## 提供指令

- `\bot` 模型对话功能:会将消息转发给模型,接收到模型的返回后输入并发送

![\\bot_1](https://github.com/gz4nna/WeChatBot/blob/master/WeChatBot.Example/bot_1.png?raw=true)

目前就这一个,其他指令还没写🤭可以先稍微将就着玩一阵子🙏

## 如何使用

首先你需要正常登录微信,进入需要用到这个小机器人(应该算吧)的群聊,然后运行这个程序即可

## 如何修改

### 模型对话功能

使用模型对话会调用`GetModelResponseAsync`方法,直接根据需要把`apiUrl`换成你的地址以及`requestData`改成你想要传的内容就好(为什么我没有放在setting里面因为懒😁)
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

后面搞个匹配专门去管理吧.现在只能在这句后面不断if去堆了

```csharp
var messageContent = lastMessage.Name;
```

## 注意事项

需要有 .NET 8 运行时