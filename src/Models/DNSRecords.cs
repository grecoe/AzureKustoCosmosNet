//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Models
{
    using Azure.ResourceManager.Dns;

    internal class DNSRecords
    {
        public DateTime Expires { get; set; } = DateTime.Now.AddMinutes(5);
        public List<DnsCnameRecordResource> CnameRecords { get; set; } = new List<DnsCnameRecordResource>();
        public List<DnsARecordResource> ARecords { get; set; } = new List<DnsARecordResource>();
    }
}
