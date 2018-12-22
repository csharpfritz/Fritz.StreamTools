using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Fritz.LiveCoding2.Exceptions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Task = System.Threading.Tasks.Task;

namespace Fritz.LiveCoding2
{

	/*
	 * Cheer ramblinggeek 100 Dec 11
	Cheer pharewings 100 
	Cheer nodebotanist 100 
	Cheer SqlMisterMagoo 600
	Cheer svavablount 100
	Cheer VindicatorVef 500
	*/



	/// <summary>
	/// This is the class that implements the package exposed by this assembly.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The minimum requirement for a class to be considered a valid package for Visual Studio
	/// is to implement the IVsPackage interface and register itself with the shell.
	/// This package uses the helper classes defined inside the Managed Package Framework (MPF)
	/// to do it: it derives from the Package class that provides the implementation of the
	/// IVsPackage interface and uses the registration attributes defined in the framework to
	/// register itself and its components with the shell. These attributes tell the pkgdef creation
	/// utility what data to put into .pkgdef file.
	/// </para>
	/// <para>
	/// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
	/// </para>
	/// </remarks>
	[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
	[Guid(CodeSuggestionsPackage.PackageGuidString)]
	[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
	[ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
	public sealed class CodeSuggestionsPackage : AsyncPackage
	{
		/// <summary>
		/// CodeSuggestionsPackage GUID string.
		/// </summary>
		public const string PackageGuidString = "a8df1642-6a10-4948-abf9-0d2fbac0753f";
		internal static IVsOutputWindow OutputWindow;

		internal static readonly Regex FileNameRegex = new Regex(@"(?<projectName>^[\w.]+\/)?(?<folders>[\w.^\/]+\/)*(?<filename>[\w.]+$)");

		/// <summary>
		/// Initializes a new instance of the <see cref="CodeSuggestionsPackage"/> class.
		/// </summary>
		public CodeSuggestionsPackage()
		{
			// Inside this method you can place any initialization code that does not require
			// any Visual Studio service because at this point the package object is created but
			// not sited yet inside Visual Studio environment. The place to do all the other
			// initialization is the Initialize method.
		}

		public static OutputWindowPane MyOutputPane { get; private set; }
		internal CodeSuggestionProxy Proxy { get; private set; }

		#region Package Members

		/// <summary>
		/// Initialization of the package; this method is called right after the package is sited, so this is the place
		/// where you can put all the initialization code that rely on services provided by VisualStudio.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
		/// <param name="progress">A provider for progress updates.</param>
		/// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
		protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
		{
			// When initialized asynchronously, the current thread may be a background thread at this point.
			// Do any initialization that requires the UI thread after switching to the UI thread.

			await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

			var thisPane = CreatePane();
			MyOutputPane = thisPane;
			WriteToPane("Code Suggestions from Twitch will appear here  \n");

			this.Proxy = new CodeSuggestionProxy();
			Proxy.OnNewCode += Proxy_OnNewCode;
			await Proxy.StartAsync();

		}

		private void Proxy_OnNewCode(object sender, CodeSuggestion e)
		{

			ThreadHelper.JoinableTaskFactory.Run(async delegate
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				// VALIDATE the filename
				IVsHierarchy projectItem = null;
				var fileName = string.Empty;


				// VALIDATE the line number
				try
				{
					(projectItem, fileName) = LocateProject(e.FileName);
				} catch (ProjectFileNotFoundException ex) {

				} catch (Exception ex) {
					// Whisper back to the person in chat about the error
				}

				WriteToPane(e, true);

				// cheer 100 svavablount  12/16/2018
				// cheer 1000 lannonbr		12/16/2018


				var p = new ErrorListProvider(this);
				var newTask = new ErrorTask
				{
					Line = e.LineNumber - 1,
					Document = fileName,
					HierarchyItem = projectItem,
					Category = TaskCategory.Misc,
					ErrorCategory = TaskErrorCategory.Message,
					Text = $"New code suggestion from {e.UserName}: \n\t {e.Body} \n"

				};
				newTask.Navigate += (s, cs) =>
				{
					newTask.Line++;
					p.Navigate(newTask, Guid.Parse(EnvDTE.Constants.vsViewKindCode));
					newTask.Line--;
				};
				p.Tasks.Add(newTask);
				p.Show();

			});


		}

		private (IVsHierarchy vsObject, string path) LocateProject(string fileName)
		{

			/// NOTE: Projects.Item and ProjectItems.Item are 1 indexed NOT 0 indexed
			///
			/// projectName? / folder* / filename
			///

			// Cheer 100 johanb 12/22/2018
			// Cheer 1500 AspiringDevOpsGuru 12/22/2018

			var normalizedFileName = fileName.Replace('\\', '/');

			// TODO: Run regex match on a background thread and timeout after 5 seconds  (maybe shorter)
			// NOTE: Need to handle if projectName matches a foldername in another project
			var match = FileNameRegex.Match(normalizedFileName);

			var projectName = match.Groups["projectName"].Success ? match.Groups["projectName"].Value.TrimEnd('/') : "";
			var folders = match.Groups["folders"].Success ? match.Groups["folders"].Value.TrimEnd('/').Split('/') : new string[0];


			var ivsSolution = (IVsSolution)Package.GetGlobalService(typeof(IVsSolution));
			var dte = (EnvDTE80.DTE2)Package.GetGlobalService(typeof(EnvDTE.DTE));

			var fileList = new List<(IVsHierarchy vsObject, string path)>();
			Project theProject = null;

			if (theProject != string.Empty)
			{

				for (var projCounter = 1; projCounter <= dte.Solution.Projects.Count; projCounter++)
				{

					var projectFound = dte.Solution.Projects.Item(projCounter).Name.Equals(projectName, StringComparison.InvariantCultureIgnoreCase);
					if (projectFound) {
						theProject = dte.Solution.Projects.Item(projCounter);
					}

				}

				if (theProject != null) projectName = string.Empty;
				fileList.AddRange(FindFiles(theProject));

			}

			if (theProject == null)
			{
				for (var projCounter = 1; projCounter <= dte.Solution.Projects.Count; projCounter++)
				{

					theProject = dte.Solution.Projects.Item(projCounter);
					fileList.AddRange(FindFiles(theProject));

				}
			}

			// TODO: Return the first... error / whisper if there are multiple
			if (fileList.Count == 0) throw new ProjectFileNotFoundException();
			if (fileList.Count > 1) throw new MultipleFilesFoundException(fileList);

			return fileList[0];

			IEnumerable<(IVsHierarchy vsObject, string path)> FindFiles(Project thisProject, ProjectItem projectItem = null)
			{

				var outList = new List<(IVsHierarchy vsObject, string path)>();
				IVsHierarchy outItem = null;

				var projectItems = projectItem != null ? projectItem.ProjectItems : thisProject.ProjectItems; 
				for (var fileCounter = 1; fileCounter <= projectItems.Count; fileCounter++)
				{

					var thisObject = projectItems.Item(fileCounter);
					if (thisObject.Name.Equals(fileName, StringComparison.InvariantCultureIgnoreCase) && thisObject.ProjectItems.Count == 0)
					{

						// NOTE: Do we need to search all of the filenames?

						ivsSolution.GetProjectOfUniqueName(thisProject.UniqueName, out outItem);
						var fullPath = thisObject.FileNames[1];
						outList.Add((outItem, fullPath));

					} else if (thisObject.ProjectItems.Count > 0) {

						outList.AddRange(FindFiles(thisProject, thisObject));

					}

				}

				return outList;

			}

		}

		internal static void WriteToPane(CodeSuggestion suggestion, bool activate = false)
		{

			//ThreadHelper.ThrowIfNotOnUIThread();
			WriteToPane($"New code suggestion from {suggestion.UserName} for {suggestion.FileName} on line {suggestion.LineNumber}: \n\t {suggestion.Body} \n", activate);

		}

		internal static void WriteToPane(string text, bool activate = false)
		{

			//ThreadHelper.ThrowIfNotOnUIThread();

			MyOutputPane.OutputString(text);
			if (activate) MyOutputPane.Activate();

		}

		private static OutputWindowPane CreatePane(string title = "Code Suggestions")
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var dte = (DTE2)AsyncPackage.GetGlobalService(typeof(DTE));
			var panes = dte.ToolWindows.OutputWindow.OutputWindowPanes;

			try
			{
				// If the pane exists already, write to it.  
				return panes.Item(title);
			}
			catch (ArgumentException)
			{
				// Create a new pane and write to it.  
				return panes.Add(title);
			}
		}

		#endregion
	}
}
