# csharp-web-portfolio## PayPal 集成说明

- **支付页面**： https://www.paypal.com/paypalme/YourAccount
- **示例代码（C#）**：

- **文档**： https://developer.paypal.com/docs/api/overview/
- **集成要点**
  1. 在 （或等价配置文件）中配置 、。
  2. 使用  进行测试，生产环境请换成 。
  3. 推荐实现 Webhook 回调来验证付款状态。

> 完成后，请在 PR 描述中注明 **PayPal 收款邮箱** ，感谢您的报酬！
## PayPal 集成说明

- 支付页面：https://www.paypal.com/paypalme/YourAccount
- 示例代码（C#）：
  var environment = new SandboxEnvironment("YOUR_CLIENT_ID", "YOUR_CLIENT_SECRET");
  var client = new PayPalHttpClient(environment);
  var order = new OrdersCreateRequest(); // 设置订单详情
  var response = await client.Execute(order);
  Console.WriteLine($"创建的订单 ID: {response.Result<Order>().Id}");
- 文档：https://developer.paypal.com/docs/api/overview/
- 集成要点：
  1. 在 appsettings.json 中配置 PayPal:ClientId、PayPal:ClientSecret。
  2. 使用 SandboxEnvironment 进行测试，生产环境换成 LiveEnvironment。
  3. 建议实现 Webhook 回调以验证付款状态。

> 完成后，请在 PR 描述中注明 PayPal 收款邮箱 517519609@qq.com，感谢您的报酬！
