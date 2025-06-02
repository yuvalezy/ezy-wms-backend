// using System;
// using Owin.Security.RedisTokenProviders;
//
// namespace Service.API;
//
// public class RefreshTokenProvider : RedisRefreshTokenProvider {
//     public RefreshTokenProvider(string redisServer) : base(new ProviderConfiguration {
//         ConnectionString   = redisServer,
//         Db                 = 0,
//         ExpiresUtc         = DateTime.UtcNow.AddMinutes(60),
//         AbortOnConnectFail = true
//     }) {
//     }
// }