using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using WebSocketSharp;

namespace FanvilMonitoring.Services;

public class FanvilService
{
    public event EventHandler<(bool status, string input)>? OnInputChanged;
    public event EventHandler<string>? OnMessageReceived;

    private bool _input0;
    private bool _input1;
    private bool _input2;

    private string _address;
    private string _username;
    private string _password;

    public FanvilService()
    {
    }

    public async Task<bool> Login(string address, string username, string password)
    {
        _address = address;
        _username = username;
        _password = password;

        var loginUrl = $"http://{address}/cgi-bin/ConfigManApp.com?key=OK";
        using var client = new HttpClient();

        var byteArray = new System.Text.UTF8Encoding().GetBytes($"{username}:{password}");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

        try
        {
            var response = await client.GetAsync(loginUrl);
            return response.StatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Request error: {e.Message}");
            return false;
        }
    }

    public void Start()
    {
        try
        {
            var ws = new WebSocket($"ws://{_address}/log");

            ws.OnMessage += (sender, e) =>
            {
                Debug.WriteLine("Received message: " + e.Data);
                HandleWebSocketMessage(e.Data);
                OnMessageReceived?.Invoke(this, e.Data);
            };
            
            ws.OnOpen += (sender, e) =>
            {
                Debug.WriteLine("Connection established");
                ws.Send("Hello, WebSocket server!");
            };
            ws.Connect();
        }
        catch (Exception ex)
        {

            ;
        }

    }

    private void HandleWebSocketMessage(string message)
    {
        if (message.Contains("input[0]"))
        {
            _input0 = !_input0;
            OnInputChanged?.Invoke(this, (_input0, "Input0"));
        }
        else if (message.Contains("input[1]"))
        {
            _input1 = !_input1;
            OnInputChanged?.Invoke(this, (_input1, "Input1"));
        }
        else if (message.Contains("input[2]"))
        {
            _input2 = !_input2;
            OnInputChanged?.Invoke(this, (_input2, "Input2"));
        }
    }

    public async Task<bool> SetOutput(int output)
    {
        var loginUrl = $"http://{_address}/cgi-bin/ConfigManApp.com?key=F_LOCK&code={output}";
        using var client = new HttpClient();

        var byteArray = new System.Text.UTF8Encoding().GetBytes($"{_username}:{_password}");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        try
        {
            var response = await client.GetAsync(loginUrl);
            return response.StatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Request error: {e.Message}");
            return false;
        }
    }
}

public enum FanvilOutput
{
    Output0,
    Output1,
    Output2
}