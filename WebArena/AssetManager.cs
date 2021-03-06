﻿using Bridge.Html5;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebArena {
	class AssetManager {
		public static Task<T> Get<T>(string name) {
			var tcs = new TaskCompletionSource<T>();
			var xhr = new XMLHttpRequest();
			xhr.Open("GET", "assets/" + name);
			xhr.OnReadyStateChange = () => {
				if(xhr.ReadyState != AjaxReadyState.Done) return;
				if(xhr.Status == 200)
					tcs.SetResult(JsonConvert.DeserializeObject<T>(xhr.ResponseText));
				else
					tcs.SetException(new Exception("XHR failed in AssetManager::Get: " + xhr.StatusText));
			};
			xhr.Send();
			return tcs.Task;
		}
	}
}
