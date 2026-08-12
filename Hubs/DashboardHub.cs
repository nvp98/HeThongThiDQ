using HeThongThiDQ.Common;
using Microsoft.AspNetCore.SignalR;

namespace HeThongThiDQ.Hubs
{
    public class DashboardHub : Hub
    {
        private readonly MyAuthentication _auth;

        public DashboardHub(MyAuthentication auth) => _auth = auth;

        public override async Task OnConnectedAsync()
        {
            // Chỉ admin (IDQuyen == 1) nhận được data push
            if (_auth.IDQuyen == 1)
                await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
            await base.OnConnectedAsync();
        }
    }
}
