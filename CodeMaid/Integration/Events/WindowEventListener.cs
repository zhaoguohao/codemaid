using EnvDTE;
using SteveCadwallader.CodeMaid.Helpers;
using System;
using System.Threading.Tasks;

namespace SteveCadwallader.CodeMaid.Integration.Events
{
    /// <summary>
    /// A class that encapsulates listening for window events.
    /// </summary>
    internal sealed class WindowEventListener : BaseEventListener
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowEventListener" /> class.
        /// </summary>
        /// <param name="package">The package hosting the event listener.</param>
        private WindowEventListener(CodeMaidPackage package)
            : base(package)
        {
            // Store access to the window events, otherwise events will not register properly via DTE.
            WindowEvents = Package.IDE.Events.WindowEvents;
        }

        /// <summary>
        /// An event raised when a window change has occurred.
        /// </summary>
        internal event Action<Document> OnWindowChange;

        /// <summary>
        /// A singleton instance of this command.
        /// </summary>
        public static WindowEventListener Instance { get; private set; }

        /// <summary>
        /// Gets or sets a pointer to the IDE window events.
        /// </summary>
        private WindowEvents WindowEvents { get; }

        /// <summary>
        /// Initializes a singleton instance of this event listener.
        /// </summary>
        /// <param name="package">The hosting package.</param>
        /// <returns>A task.</returns>
        public static async Task InitializeAsync(CodeMaidPackage package)
        {
            Instance = new WindowEventListener(package);
            package.SettingsMonitor.Watch(s => s.Feature_SpadeToolWindow, Instance.SwitchAsync);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release
        /// only unmanaged resources.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                IsDisposed = true;

                if (disposing && WindowEvents != null)
                {
                    await SwitchAsync(on: false);
                }
            }
        }

        /// <summary>
        /// Registers event handlers with the IDE.
        /// </summary>
        /// <returns>A task.</returns>
        protected override async Task RegisterListenersAsync()
        {
            WindowEvents.WindowActivated += WindowEvents_WindowActivated;
        }

        /// <summary>
        /// Unregisters event handlers with the IDE.
        /// </summary>
        /// <returns>A task.</returns>
        protected override async Task UnRegisterListenersAsync()
        {
            WindowEvents.WindowActivated -= WindowEvents_WindowActivated;
        }

        /// <summary>
        /// Raises the window change event.
        /// </summary>
        /// <param name="document">The document that got focus, may be null.</param>
        private void RaiseWindowChange(Document document)
        {
            var onWindowChange = OnWindowChange;
            if (onWindowChange != null)
            {
                OutputWindowHelper.DiagnosticWriteLine($"WindowEventListener.OnWindowChange raised for '{(document != null ? document.FullName : "(null)")}'");

                onWindowChange(document);
            }
        }

        /// <summary>
        /// An event handler for a window being activated.
        /// </summary>
        /// <param name="gotFocus">The window that got focus.</param>
        /// <param name="lostFocus">The window that lost focus.</param>
        private void WindowEvents_WindowActivated(Window gotFocus, Window lostFocus)
        {
            if (gotFocus.Kind == "Document")
            {
                RaiseWindowChange(gotFocus.Document);
            }
            else if (Package.ActiveDocument == null)
            {
                RaiseWindowChange(null);
            }
        }
    }
}