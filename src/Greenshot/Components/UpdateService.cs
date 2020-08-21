// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.OwnedInstances;
using Caliburn.Micro;
using Dapplo.Addons;
using Dapplo.CaliburnMicro;
using Dapplo.HttpExtensions;
using Dapplo.Log;
using Greenshot.Addons.Core;
using Greenshot.Ui.Notifications.ViewModels;

namespace Greenshot.Components
{
    /// <summary>
    ///     This processes the information, if there are updates available.
    /// </summary>
    [Service(nameof(UpdateService), nameof(MainFormStartup))]
    public class UpdateService : IStartup, IShutdown, IVersionProvider
    {
        private static readonly LogSource Log = new LogSource();
        private static readonly Regex VersionRegex = new Regex(@"^.*[^-]-(?<version>[0-9\.]+)\-(?<type>(release|beta|rc[0-9]+))\.exe.*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Uri UpdateFeed = new Uri("https://getgreenshot.org/project-feed/");
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly ICoreConfiguration _coreConfiguration;
        private readonly IEventAggregator _eventAggregator;
        private readonly Func<Version, Owned<UpdateNotificationViewModel>> _updateNotificationViewModelFactory;

        /// <inheritdoc />
        public Version CurrentVersion { get; }

        /// <inheritdoc />
        public Version LatestVersion { get; private set; }

        /// <summary>
        /// The latest beta version
        /// </summary>
        public Version BetaVersion { get; private set; }

        /// <summary>
        /// The latest RC version
        /// </summary>
        public Version ReleaseCandidateVersion { get; private set; }

        /// <inheritdoc />
        public bool IsUpdateAvailable => LatestVersion > CurrentVersion;

        /// <summary>
        /// Constructor with dependencies
        /// </summary>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="eventAggregator">IEventAggregator</param>
        /// <param name="updateNotificationViewModelFactory">UpdateNotificationViewModel factory</param>
        public UpdateService(
            ICoreConfiguration coreConfiguration,
            IEventAggregator eventAggregator,
            Func<Version, Owned<UpdateNotificationViewModel>> updateNotificationViewModelFactory)
        {
            _coreConfiguration = coreConfiguration;
            _eventAggregator = eventAggregator;
            _updateNotificationViewModelFactory = updateNotificationViewModelFactory;
            var version = FileVersionInfo.GetVersionInfo(GetType().Assembly.Location);
            LatestVersion = CurrentVersion = new Version(version.FileMajorPart, version.FileMinorPart, version.FileBuildPart);
            if (_coreConfiguration != null)
            {
                _coreConfiguration.LastSaveWithVersion = CurrentVersion.ToString();
            }
        }

        /// <inheritdoc />
        public void Shutdown()
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }
        }

        public void Startup()
        {
        }
    }
}