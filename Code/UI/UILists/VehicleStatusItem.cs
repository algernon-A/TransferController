// <copyright file="VehicleStatusItem.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    /// <summary>
    /// Vehicle list item record.
    /// </summary>
    public class VehicleStatusItem
    {
        // Vehicle data
        private ushort _vehicleID;
        private ushort _targetBuildingID;
        private ushort _amount;
        private string _vehicleName;
        private TransferManager.TransferReason _material;

        /// <summary>
        /// Initializes a new instance of the <see cref="VehicleStatusItem"/> class.
        /// </summary>
        /// <param name="vehicleID">Vehicle ID.</param>
        /// <param name="vehicleInfo">Vehicle prefab.</param>
        /// <param name="targetBuildingID">Vehicle target building.</param>
        /// <param name="material">Vehicle transfer material.</param>
        /// <param name="amount">Vehicle transfer amount.</param>
        public VehicleStatusItem(ushort vehicleID, VehicleInfo vehicleInfo, ushort targetBuildingID, byte material, ushort amount)
        {
            _vehicleID = vehicleID;
            _vehicleName = TextUtils.GetDisplayName(vehicleInfo);
            _targetBuildingID = targetBuildingID;
            _material = (TransferManager.TransferReason)material;
            _amount = amount;
        }

        /// <summary>
        /// Gets the vehicle's ID.
        /// </summary>
        public ushort VehicleID => _vehicleID;

        /// <summary>
        /// Gets the vehicle's target building ID.
        /// </summary>
        public ushort TargetBuildingID => _targetBuildingID;

        /// <summary>
        /// Gets the vehicle's cargo amount.
        /// </summary>
        public ushort Amount => _amount;

        /// <summary>
        /// Gets the vehicle's name.
        /// </summary>
        public string Name => _vehicleName;

        /// <summary>
        /// Gets the vehicle's transfer material.
        /// </summary>
        public TransferManager.TransferReason Material => _material;
    }
}