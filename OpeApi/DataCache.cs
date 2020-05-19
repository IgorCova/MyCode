using Api.Extensions;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;

namespace Api.Classes {
  #region Base DataCache
  internal class DataCache {
    private static IMemoryCache memoryCache;
    private static TimeSpan offsetCache;
    internal static void SetMemoryCache(IMemoryCache cache) {
      memoryCache = cache;
      offsetCache = TimeSpan.FromDays(1);
    }

    internal static void SetExpiration(TimeSpan offset) {
      offsetCache = offset;
    }

    internal static void SaveToCashe(string key, string value) {
      MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions()
          .SetSlidingExpiration(offsetCache);

      memoryCache.Set(key, value, cacheEntryOptions);
    }

    internal static bool ReadFromCache(string key, out string value) {
      return memoryCache.TryGetValue(key, out value);
    }
  }
  #endregion Base DataCache

  internal class ApiUserCache : DataCache {
    internal static void Get_UserData(string username, ref ApiUser apiUser) {
      if (ReadFromCache(username, out string value)) {
        apiUser.FromJson(value);
      } else {
        DataAccess.Get_UserData(username, out string user_public_key, out int id_user);

        apiUser.PublicKey = user_public_key;
        apiUser.User_ID = id_user;

        value = apiUser.ToJson();

        SaveToCashe(username, value);
      }
    }

    internal static void Init_UserData() {
      IList<ApiUserExtended> users = DataAccess.Get_ApiUsers();
      foreach (ApiUserExtended user in users) {
        string value = user.ToJson();
        SaveToCashe(user.GetUsername(), value);
      }
    }
  }
}