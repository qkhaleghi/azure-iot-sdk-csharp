// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Authentication method that uses a shared access signature token and allows for token refresh.
    /// </summary>
    public sealed class DeviceAuthenticationWithTpm : DeviceAuthenticationWithTokenRefresh
    {
        private SecurityProviderTpm _securityProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceAuthenticationWithTpm"/> class with default
        /// time to live of 1 hour and default buffer percentage value of 15.
        /// </summary>
        /// <param name="deviceId">Device Identifier.</param>
        /// <param name="securityProvider">Device Security Provider settings for TPM Hardware Security Modules.</param>
        public DeviceAuthenticationWithTpm(
            string deviceId,
            SecurityProviderTpm securityProvider) : base(deviceId)
        {
            _securityProvider = securityProvider ?? throw new ArgumentNullException(nameof(securityProvider));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceAuthenticationWithTpm"/> class.
        /// </summary>
        /// <param name="deviceId">Device Identifier.</param>
        /// <param name="securityProvider">Device Security Provider settings for TPM Hardware Security Modules.</param>
        /// <param name="suggestedTimeToLiveSeconds">Token time to live suggested value.</param>
        /// <param name="timeBufferPercentage">Time buffer before expiry when the token should be renewed expressed as percentage of
        /// the time to live. EX: If you want a SAS token to live for 85% of life before proactive renewal, this value should be 15.</param>
        public DeviceAuthenticationWithTpm(
            string deviceId,
            SecurityProviderTpm securityProvider,
            int suggestedTimeToLiveSeconds,
            int timeBufferPercentage) : base(deviceId, suggestedTimeToLiveSeconds, timeBufferPercentage)
        {
            _securityProvider = securityProvider ?? throw new ArgumentNullException(nameof(securityProvider));
        }

        ///<inheritdoc/>
        protected override Task<string> SafeCreateNewToken(string iotHub, int suggestedTimeToLiveSeconds)
        {
            var builder = new TpmSharedAccessSignatureBuilder(_securityProvider)
            {
                TimeToLive = TimeSpan.FromSeconds(suggestedTimeToLiveSeconds),
                Target = "{0}/devices/{1}".FormatInvariant(
                    iotHub,
                    WebUtility.UrlEncode(DeviceId)),
            };

            return Task.FromResult(builder.ToSignature());
        }

        private class TpmSharedAccessSignatureBuilder : SharedAccessSignatureBuilder
        {
            private SecurityProviderTpm _securityProvider;

            public TpmSharedAccessSignatureBuilder(SecurityProviderTpm securityProvider)
            {
                _securityProvider = securityProvider;
            }

            protected override string Sign(string requestString, string key)
            {
                Debug.Assert(key == null);

                byte[] encodedBytes = Encoding.UTF8.GetBytes(requestString);
                byte[] hmac = _securityProvider.Sign(encodedBytes);
                return Convert.ToBase64String(hmac);
            }
        }
    }
}
