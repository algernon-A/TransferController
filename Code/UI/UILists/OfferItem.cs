// <copyright file="OfferItem.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    /// <summary>
    /// Class to hold offer data from open offers.
    /// </summary>
    public class OfferItem
    {
        private TransferManager.TransferReason _reason;
        private byte _priority;
        private bool _incoming;

        /// <summary>
        /// Initializes a new instance of the <see cref="OfferItem"/> class.
        /// </summary>
        /// <param name="reason">Transfer reason.</param>
        /// <param name="priority">Offer priority.</param>
        /// <param name="incoming">Incoming status.</param>
        public OfferItem(TransferManager.TransferReason reason, byte priority, bool incoming)
        {
            _reason = reason;
            _priority = priority;
            _incoming = incoming;
        }

        /// <summary>
        /// Gets the row's transfer reason.
        /// </summary>
        public TransferManager.TransferReason Reason => _reason;

        /// <summary>
        /// Gets the row's transfer priority.
        /// </summary>
        public byte Priority => _priority;

        /// <summary>
        /// Gets a value indicating whether the transfer was incoming (true) or outgoing (false).
        /// </summary>
        public bool IsIncoming => _incoming;
    }
}