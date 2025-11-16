using Microsoft.Extensions.Caching.Memory;

namespace Gateway.Infrastructure.Cache
{
    public class TokenCacheService
    {
        private readonly IMemoryCache _cache;
        private readonly int _expirationMinutes;

        public TokenCacheService(IMemoryCache cache)
        {
            _cache = cache;
            _expirationMinutes = 60; //Se puede configurar en appsettings.json
        }

        //Obtener el token desde la caché
        public string GetToken(string key)
        {
            _cache.TryGetValue(key, out string token);
            return token;
        }

        //Almacenar el token en caché
        public void StoreToken(string key, string token)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(_expirationMinutes));

            _cache.Set(key, token, cacheEntryOptions);
        }
    }
}
