using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class SimpleNetworkDiscovery : MonoBehaviour
{
    [SerializeField] private int discoveryPort = 47777;
    [SerializeField] private string discoveryMessage = "DarTennisVR_Room"; // 우리 게임 식별자

    private UdpClient udpClient;
    private bool isBroadcasting = false;
    private bool isSearching = false;

    /// <summary>
    /// [Host] 내 방을 로컬 네트워크에 알리기 시작
    /// </summary>
    public void StartBroadcasting()
    {
        StopDiscovery(); // 기존 작업 중단
        
        try
        {
            udpClient = new UdpClient();
            udpClient.EnableBroadcast = true;
            isBroadcasting = true;
            BroadcastLoop();
            Debug.Log("[Discovery] Broadcasting started...");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Discovery] Failed to start broadcasting: {e.Message}");
        }
    }

    private async void BroadcastLoop()
    {
        var endPoint = new IPEndPoint(IPAddress.Broadcast, discoveryPort);
        var data = Encoding.UTF8.GetBytes(discoveryMessage);

        while (isBroadcasting && udpClient != null)
        {
            try
            {
                // 1초마다 "나 여기 있어" 메시지 전송
                await udpClient.SendAsync(data, data.Length, endPoint);
                await Task.Delay(1000); 
            }
            catch
            {
                break;
            }
        }
    }

    /// <summary>
    /// [Client] 주변에 방이 있는지 탐색
    /// </summary>
    public void SearchForServer(Action<string> onServerFound, float duration = 2.0f)
    {
        StopDiscovery();
        StartCoroutine(SearchRoutine(onServerFound, duration));
    }

    private System.Collections.IEnumerator SearchRoutine(Action<string> onServerFound, float duration)
    {
        isSearching = true;
        // 수신용 포트 바인딩
        try 
        {
            udpClient = new UdpClient(discoveryPort);
            udpClient.EnableBroadcast = true;
        }
        catch(Exception e)
        {
            Debug.LogWarning($"[Discovery] 포트 바인딩 실패 (이미 사용 중일 수 있음): {e.Message}");
            onServerFound?.Invoke(null);
            yield break;
        }
        
        // 비동기 수신 시작
        var resultTask = ReceiveAsync();
        
        float endTime = Time.time + duration;
        while (Time.time < endTime)
        {
            if (resultTask.IsCompleted)
            {
                var result = resultTask.Result;
                if (!string.IsNullOrEmpty(result))
                {
                    Debug.Log($"[Discovery] Server found at {result}");
                    onServerFound?.Invoke(result);
                    StopDiscovery();
                    yield break;
                }
            }
            yield return null;
        }

        Debug.Log("[Discovery] No server found.");
        onServerFound?.Invoke(null); // 못 찾음
        StopDiscovery();
    }

    private async Task<string> ReceiveAsync()
    {
        try
        {
            while (isSearching && udpClient != null)
            {
                var result = await udpClient.ReceiveAsync();
                var message = Encoding.UTF8.GetString(result.Buffer);
                
                // 우리 게임의 메시지가 맞는지 확인
                if (message == discoveryMessage)
                {
                    // 찾은 방의 IP 주소 반환
                    return result.RemoteEndPoint.Address.ToString();
                }
            }
        }
        catch
        {
            // Timeout or Close
        }
        return null;
    }

    public void StopDiscovery()
    {
        isBroadcasting = false;
        isSearching = false;
        udpClient?.Close();
        udpClient = null;
    }

    private void OnDestroy()
    {
        StopDiscovery();
    }
}
