﻿using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;

namespace OpenSim.Data
{
    public interface IRegionProfileService
    {
        /// <summary>
        /// Request sim profile information from a grid server, by Region UUID
        /// </summary>
        /// <param name="regionId">The region UUID to look for</param>
        /// <param name="gridserverUrl"></param>
        /// <param name="gridserverSendkey"></param>
        /// <param name="gridserverRecvkey"></param>
        /// <returns>The sim profile.  Null if there was a request failure</returns>
        /// <remarks>This method should be statics</remarks>
        RegionProfileData RequestSimProfileData(UUID regionId, Uri gridserverUrl,
                                                       string gridserverSendkey, string gridserverRecvkey);

        /// <summary>
        /// Request sim profile information from a grid server, by Region Handle
        /// </summary>
        /// <param name="region_handle">the region handle to look for</param>
        /// <param name="gridserver_url"></param>
        /// <param name="gridserver_sendkey"></param>
        /// <param name="gridserver_recvkey"></param>
        /// <returns>The sim profile.  Null if there was a request failure</returns>
        RegionProfileData RequestSimProfileData(ulong region_handle, Uri gridserver_url,
                                                       string gridserver_sendkey, string gridserver_recvkey);

        /// <summary>
        /// Request sim profile information from a grid server, by Region Name
        /// </summary>
        /// <param name="regionName">the region name to look for</param>
        /// <param name="gridserverUrl"></param>
        /// <param name="gridserverSendkey"></param>
        /// <param name="gridserverRecvkey"></param>
        /// <returns>The sim profile.  Null if there was a request failure</returns>
        RegionProfileData RequestSimProfileData(string regionName, Uri gridserverUrl,
                                                       string gridserverSendkey, string gridserverRecvkey);
    }
}