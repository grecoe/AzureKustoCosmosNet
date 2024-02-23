//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Domain
{
    using Azure.ResourceManager;
    using Azure.ResourceManager.Resources;

    internal class AzureResourceGroup : RawResourceGroup
    {
        private const string ExpirationTag = "expiration";
        private const string DeletionAttemptTag = "deleteattempted";
        private const string IsProtectedTag = "delete";

        public bool HasExpiration
        {
            get
            {
                return this.Tags.ContainsKey(ExpirationTag);
            }
        }

        public bool IsManaged
        {
            get
            {
                return string.IsNullOrEmpty(this.ManagedBy) == false;
            }
        }

        public bool IsProtected
        {
            get
            {
                bool isProtected = false;

                if (this.Tags.ContainsKey(AzureResourceGroup.IsProtectedTag))
                {
                    isProtected = (
                        this.Tags[AzureResourceGroup.IsProtectedTag].ToLower() ==
                        "false");
                }

                return isProtected;
            }
        }

        public bool DeleteionAttempted
        {
            get
            {
                return this.Tags.ContainsKey(AzureResourceGroup.DeletionAttemptTag);
            }
        }

        public bool IsExpired
        {
            get
            {
                bool isExpired = false;

                if( this.Tags.ContainsKey(ExpirationTag))
                {
                    DateTime val = DateTime.Parse(this.Tags[AzureResourceGroup.ExpirationTag]);
                    isExpired = val <= DateTime.UtcNow;
                }

                return isExpired;
            }
        }

        public DateTime? ExpirationDate
        {
            get
            {
                DateTime? expiration = null;

                if (this.Tags.ContainsKey(ExpirationTag))
                {
                    expiration = DateTime.Parse(this.Tags[AzureResourceGroup.ExpirationTag]);
                }

                return expiration;
            }
        }

        public AzureResourceGroup(ResourceGroupResource resourceGroup, ArmClient client) 
            : base(resourceGroup, client)
        {
        }

        public bool SetExpiration(DateTime utcExpiration)
        {
            if( this.HasExpiration)
            {
                this.Tags[AzureResourceGroup.ExpirationTag] = utcExpiration.ToString();
            }
            else
            {
                this.Tags.Add(AzureResourceGroup.ExpirationTag, utcExpiration.ToString());
            }
            return this.UpdateTags();
        }

        public bool SetDeletionAttempt()
        {
            string recordTime = DateTime.UtcNow.ToString();
            if( this.Tags.ContainsKey(DeletionAttemptTag))
            {
                this.Tags[DeletionAttemptTag] = recordTime;
            }
            else
            {
                this.Tags.Add(DeletionAttemptTag, recordTime);
            }
            return this.UpdateTags();
        }
    }
}
