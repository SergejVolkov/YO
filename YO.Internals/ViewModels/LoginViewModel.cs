using System;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using YO.Internals.Cache;
using YO.Internals.Configuration;
using YO.Internals.Extensions;
using YO.Internals.Shikimori;
using YO.Internals.Shikimori.Data;

namespace YO.Internals.ViewModels
{
	public class LoginViewModel : ViewModelBase
	{
		private readonly IConfiguration _configuration;
		private readonly IShikimoriApi _shikimoriApi;
		private readonly IImageCache _imageCache;
		
		public LoginViewModel(IConfiguration configuration,
							  IShikimoriApi shikimoriApi,
							  IImageCache imageCache)
		{
			_configuration = configuration;
			_shikimoriApi = shikimoriApi;
			_imageCache = imageCache;
			
			_configuration.WhenAnyValue(c => c.ShikimoriUsername)
						  .WhereNullOrEmpty()
						  .SubscribeDiscard(OnNullUser);
			_configuration.WhenAnyValue(c => c.ShikimoriUsername)
						  .WhereNotNull()
						  .SubscribeAsync(OnUserNameChanged);
			
			this.WhenPropertyChanged(t => t.UserInfo)
				.WhereNullValues()
				.SubscribeDiscard(OnNullUser);
			this.WhenPropertyChanged(t => t.UserInfo)
				.WhereNotNullValues()
				.Subscribe(OnUserInfoChanged);

			ShikimoriUsername = _configuration.ShikimoriUsername;
			Confirm = ReactiveCommand.Create(ConfirmImpl);
		}
		
		[Reactive]
		public string? ShikimoriUsername { get; set; }

		[Reactive]
		public User? UserInfo { get; private set; }

		[Reactive]
		public Bitmap? UserPicture { get; private set; }

		[Reactive]
		public bool IsAuthorized { get; private set; }
		
		public ReactiveCommand<Unit, Unit> Confirm { get; }

		private void ConfirmImpl()
		{
			_configuration.ShikimoriUsername = ShikimoriUsername;
		}
		
		private async Task OnUserNameChanged(string userName)
		{
			UserInfo = await _shikimoriApi.Users.GetByNickname(userName);
		}

		private void OnUserInfoChanged(User userInfo)
		{
			IsAuthorized = true;
			UserPicture = _imageCache.TryGetUserPicture(userInfo);
		}

		private void OnNullUser()
		{
			if (UserInfo is not null)
			{
				UserInfo = null;
			}
			
			IsAuthorized = false;
			UserPicture = null;
		}
	}
}