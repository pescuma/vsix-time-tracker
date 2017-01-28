//------------------------------------------------------------------------------
// <copyright file="VSIXTimeTrackerPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace VSIXTimeTracker
{
	/// <summary>
	///     This is the class that implements the package exposed by this assembly.
	/// </summary>
	/// <remarks>
	///     <para>
	///         The minimum requirement for a class to be considered a valid package for Visual Studio
	///         is to implement the IVsPackage interface and register itself with the shell.
	///         This package uses the helper classes defined inside the Managed Package Framework (MPF)
	///         to do it: it derives from the Package class that provides the implementation of the
	///         IVsPackage interface and uses the registration attributes defined in the framework to
	///         register itself and its components with the shell. These attributes tell the pkgdef creation
	///         utility what data to put into .pkgdef file.
	///     </para>
	///     <para>
	///         To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...
	///         &gt; in .vsixmanifest file.
	///     </para>
	/// </remarks>
	[PackageRegistration(UseManagedResourcesOnly = true)]
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
	[Guid(PackageGuidString)]
	[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly",
		Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
	public sealed class VSIXTimeTrackerPackage : Package
	{
		/// <summary>
		///     VSIXTimeTrackerPackage GUID string.
		/// </summary>
		public const string PackageGuidString = "58dd4b44-bfaf-438b-bac5-7afd515faf77";

		private IVsOutputWindowPane outputWindow;

		#region Package Members

		/// <summary>
		///     Initialization of the package; this method is called right after the package is sited, so this is the place
		///     where you can put all the initialization code that rely on services provided by VisualStudio.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();

			CreateOutputWindow();
		}

		private void CreateOutputWindow()
		{
			var outWindow = (IVsOutputWindow) GetGlobalService(typeof(SVsOutputWindow));

			var customGuid = new Guid("7618E147-6436-4A9D-B6D5-06527EA915A8");
			outWindow.CreatePane(ref customGuid, "Time Tracker", 1, 1);

			outWindow.GetPane(ref customGuid, out outputWindow);
		}

		#endregion
	}
}