using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Models.AppSettings
{
    internal class  DNSEnvironment
    {
        /// <summary>
        /// Product environments supported by this DNS Zone
        /// </summary>
        public List<string> Environments { get; set; } = new List<string>();
        /// <summary>
        /// Subscription the DNS Zone lives in.
        /// </summary>
        public string Subscription { get; set; } = string.Empty;
        /// <summary>
        /// Resource Group the DNS Zone lives in.
        /// </summary>
        public string ResourceGroup { get; set; } = string.Empty;
        /// <summary>
        /// DNS Zone itself.
        /// </summary>
        public string ZoneName { get; set; } = string.Empty;
       
    }

    internal class DNSSettings
    {
        public const string SECTION = "DNS";

        /// <summary>
        /// Available/acceptable DNS zones that are here, should be Prod and NonProd but
        /// is extensible. Will have to work in the SvcHostUtils file if modifcations to it 
        /// are made.
        /// </summary>
        public List<string> AcceptableInstanceEnvironment { get; set; } = new List<string>();

        /// <summary>
        /// The different DNS Zones configured in appsettings.
        /// </summary>
        public List<DNSEnvironment> Environments { get; set; } = new List<DNSEnvironment>();
    }
}
