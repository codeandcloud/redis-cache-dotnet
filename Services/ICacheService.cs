using System.Text.Json;
using StackExchange.Redis;

namespace CachingWebApi.Services;

public interface ICacheService
{
    T GetData<T>(string key);
    bool RemoveData(string key);
    bool SetData<T>(string key, T data, DateTimeOffset expirationTime);
}

public class CacheService : ICacheService
{
    private readonly IDatabase _cacheDb;

    public CacheService()
    {
        var redis = ConnectionMultiplexer.Connect("localhost:6379");
        _cacheDb = redis.GetDatabase();
    }

    public T GetData<T>(string key)
    {
        var data = _cacheDb.StringGet(key);
        return data.HasValue ? JsonSerializer.Deserialize<T>(data) : default;
    }

    public bool RemoveData(string key)
    {
        if(_cacheDb.KeyExists(key))
        {
            return _cacheDb.KeyDelete(key);
        }
        return false;
    }

    public bool SetData<T>(string key, T data, DateTimeOffset expirationTime)
    {
        var serializedData = JsonSerializer.Serialize(data);
        var expiry = expirationTime.DateTime.Subtract(DateTime.Now);
        
        return _cacheDb.StringSet(key, serializedData, expiry);
    }
}