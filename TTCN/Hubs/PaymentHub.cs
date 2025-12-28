using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

public class PaymentHub : Hub
{
    public async Task JoinShowGroup(int maSuat)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"SUAT_{maSuat}");
    }

    public async Task HoldSeat(int maSuat, int maGhe)
    {
        await Clients.OthersInGroup($"SUAT_{maSuat}")
            .SendAsync("SeatHeld", new { maGhe });
    }

    public async Task ReleaseSeat(int maSuat, int maGhe)
    {
        await Clients.Group($"SUAT_{maSuat}")
            .SendAsync("SeatReleased", new { maGhe });
    }

    public async Task SeatSold(int maSuat, int maGhe)
    {
        await Clients.Group($"SUAT_{maSuat}")
            .SendAsync("SeatSold", new { maGhe });
    }
}
