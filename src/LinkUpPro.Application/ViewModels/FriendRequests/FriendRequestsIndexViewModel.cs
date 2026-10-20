namespace LinkUpPro.Application.ViewModels.FriendRequests
{
    public class FriendRequestsIndexViewModel
    {
        public List<FriendRequestViewModel> Pendientes { get; set; } = new();
        public List<FriendRequestViewModel> Enviadas { get; set; } = new();
    }
}
