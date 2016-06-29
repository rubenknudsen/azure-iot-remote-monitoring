﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Applications.RemoteMonitoring.Common.Models;
using Microsoft.Azure.Devices.Applications.RemoteMonitoring.DeviceAdmin.Infrastructure.Models;

namespace Microsoft.Azure.Devices.Applications.RemoteMonitoring.DeviceAdmin.Infrastructure.Repository
{
    /// <summary>
    /// Sample implemementation of Device Type data
    /// In a real world implementation this data may be backed by storage such as Table Storage or SQL Server
    /// </summary>
    public class SampleDeviceTypeRepository : IDeviceTypeRepository
    {
        List<DeviceType> DeviceTypes = new List<DeviceType>
        {
            new DeviceType 
            {
                Name = Strings.SimulatedDeviceName,
                DeviceTypeId = DeviceTypeConstants.SIMULATED,
                Description = Strings.SimulatedDeviceDescription,
                InstructionsUrl = null,
                IsSimulatedDevice = true
            },
            new DeviceType 
            {
                Name = Strings.CustomDeviceName,
                DeviceTypeId = DeviceTypeConstants.CUSTOM,
                Description = Strings.CustomDeviceDescription,
                InstructionsUrl = Strings.CustomDeviceInstructionsUrl
            },
            new DeviceType
            {
                Name = Strings.MbedDeviceName,
                DeviceTypeId = DeviceTypeConstants.MBED,
                Description = Strings.MbedDeviceDescription,
                InstructionsUrl = Strings.CustomDeviceInstructionsUrl,
                IsSimulatedDevice = false
            }
        };

        /// <summary>
        /// Return the full list of device types available
        /// </summary>
        /// <returns></returns>
        public async Task<List<DeviceType>> GetAllDeviceTypesAsync()
        {
            return await Task.Run(() => { return DeviceTypes; });
        }

        /// <summary>
        /// Return a single device type
        /// </summary>
        /// <param name="deviceTypeId"></param>
        /// <returns></returns>
        public async Task<DeviceType> GetDeviceTypeAsync(int deviceTypeId)
        {
            return await Task.Run(() =>
            {
                return DeviceTypes.FirstOrDefault(dt => dt.DeviceTypeId == deviceTypeId);
            });
        }

    }

}
