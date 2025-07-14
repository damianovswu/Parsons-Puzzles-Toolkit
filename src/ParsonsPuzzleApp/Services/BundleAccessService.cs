using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ParsonsPuzzleApp.Services
{
    public interface IBundleAccessService
    {
        void GrantAccess(int bundleId, string studentIdentifier);
        bool HasAccess(int bundleId, string studentIdentifier);
        void RevokeAccess(int bundleId);
        void RevokeAllAccess();
    }

    public class BundleAccessService : IBundleAccessService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string SessionKey = "BundleAccess";

        public BundleAccessService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void GrantAccess(int bundleId, string studentIdentifier)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null) return;

            var accessList = GetAccessList();
            var accessKey = $"{bundleId}:{studentIdentifier}";

            if (!accessList.Contains(accessKey))
            {
                accessList.Add(accessKey);
                SaveAccessList(accessList);
            }

            session.SetString($"BundleAccess_{bundleId}_Time", DateTime.UtcNow.ToString("O"));
        }

        public bool HasAccess(int bundleId, string studentIdentifier)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null) return false;

            var accessList = GetAccessList();
            var accessKey = $"{bundleId}:{studentIdentifier}";

            if (!accessList.Contains(accessKey))
                return false;

            var timeStr = session.GetString($"BundleAccess_{bundleId}_Time");
            if (DateTime.TryParse(timeStr, out var grantedTime))
            {
                // Access expires after 24 hours
                if (DateTime.UtcNow.Subtract(grantedTime).TotalHours > 24)
                {
                    RevokeAccess(bundleId);
                    return false;
                }
            }

            return true;
        }

        public void RevokeAccess(int bundleId)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null) return;

            var accessList = GetAccessList();
            accessList.RemoveAll(a => a.StartsWith($"{bundleId}:"));
            SaveAccessList(accessList);

            session.Remove($"BundleAccess_{bundleId}_Time");
        }

        public void RevokeAllAccess()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null) return;

            session.Remove(SessionKey);

            var keys = session.Keys.Where(k => k.StartsWith("BundleAccess_") && k.EndsWith("_Time")).ToList();
            foreach (var key in keys)
            {
                session.Remove(key);
            }
        }

        private List<string> GetAccessList()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null) return new List<string>();

            var json = session.GetString(SessionKey);
            if (string.IsNullOrEmpty(json))
                return new List<string>();

            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }

        private void SaveAccessList(List<string> accessList)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null) return;

            var json = JsonSerializer.Serialize(accessList);
            session.SetString(SessionKey, json);
        }
    }
}