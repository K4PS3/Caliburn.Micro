﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace Caliburn.Micro
{
    /// <summary>
    /// A base implementation of <see cref = "IScreen" />.
    /// </summary>
    public class Screen : ViewAware, IScreen, IChild
    {
        private static readonly ILog Log = LogManager.GetLog(typeof(Screen));
        private string _displayName;

        private bool _isActive;
        private bool _isInitialized;
        private object _parent;

        /// <summary>
        /// Creates an instance of the screen.
        /// </summary>
        public Screen()
        {
            _displayName = GetType().FullName;
        }

        /// <summary>
        /// Indicates whether or not this instance is currently initialized.
        /// Virtualized in order to help with document oriented view models.
        /// </summary>
        public virtual bool IsInitialized
        {
            get => _isInitialized;
            private set
            {
                _isInitialized = value;
                NotifyOfPropertyChange();
            }
        }

        /// <summary>
        /// Gets or Sets the Parent <see cref = "IConductor" />
        /// </summary>
        public virtual object Parent
        {
            get => _parent;
            set
            {
                _parent = value;
                NotifyOfPropertyChange();
            }
        }

        /// <summary>
        /// Gets or Sets the Display Name
        /// </summary>
        public virtual string DisplayName
        {
            get => _displayName;
            set
            {
                _displayName = value;
                NotifyOfPropertyChange();
            }
        }

        /// <summary>
        /// Indicates whether or not this instance is currently active.
        /// Virtualized in order to help with document oriented view models.
        /// </summary>
        public virtual bool IsActive
        {
            get => _isActive;
            private set
            {
                _isActive = value;
                NotifyOfPropertyChange();
            }
        }

        /// <summary>
        /// Raised after activation occurs.
        /// </summary>
        public virtual event AsyncEventHandler<ActivationEventArgs> Activated = delegate { return Task.FromResult(true); };

        /// <summary>
        /// Raised before deactivation.
        /// </summary>
        public virtual event EventHandler<DeactivationEventArgs> AttemptingDeactivation = delegate { };

        /// <summary>
        /// Raised after deactivation.
        /// </summary>
        public virtual event AsyncEventHandler<DeactivationEventArgs> Deactivated = delegate { return Task.FromResult(true); };

        async Task IActivate.ActivateAsync(CancellationToken cancellationToken)
        {
            Log.Info("Activating async {0}.", this.DisplayName);
            if (IsActive)
                return;

            var initialized = false;

            if (!IsInitialized)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                await OnInitializeAsync(cancellationToken);
#pragma warning restore CS0618 // Type or member is obsolete
                IsInitialized = initialized = true;
                await OnInitializedAsync(cancellationToken);
            }

            Log.Info("Activating {0}.", this);
#pragma warning disable CS0618 // Type or member is obsolete
            await OnActivateAsync(cancellationToken);
#pragma warning restore CS0618 // Type or member is obsolete
            IsActive = true;
            await OnActivatedAsync(cancellationToken);

            await (Activated?.InvokeAllAsync(this, new ActivationEventArgs
            {
                WasInitialized = initialized
            }) ?? Task.FromResult(true));
        }

        async Task IDeactivate.DeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            Log.Info("Deactivating async {0}.", this.DisplayName);
            if (IsActive || IsInitialized && close)
            {
                AttemptingDeactivation?.Invoke(this, new DeactivationEventArgs
                {
                    WasClosed = close
                });

                Log.Info("Deactivating {0}.", this);
                await OnDeactivateAsync(close, cancellationToken);
                IsActive = false;

                await (Deactivated?.InvokeAllAsync(this, new DeactivationEventArgs
                {
                    WasClosed = close
                }) ?? Task.FromResult(true));

                if (close)
                {
                    Views.Clear();
                    Log.Info("Closed {0}.", this);
                }
            }
        }

        /// <summary>
        /// Called to check whether or not this instance can close.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>A task that represents the asynchronous operation and holds the value of the close check..</returns>
        public virtual Task<bool> CanCloseAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// Tries to close this instance by asking its Parent to initiate shutdown or by asking its corresponding view to close.
        /// Also provides an opportunity to pass a dialog result to it's corresponding view.
        /// </summary>
        /// <param name="dialogResult">The dialog result.</param>
        public virtual async Task TryCloseAsync(bool? dialogResult = null)
        {
            if (Parent is IConductor conductor)
            {
                await conductor.CloseItemAsync(this, CancellationToken.None);
            }

            var closeAction = PlatformProvider.Current.GetViewCloseAction(this, Views.Values, dialogResult);

            await Execute.OnUIThreadAsync(async () => await closeAction(CancellationToken.None));
        }

        /// <summary>
        /// Called when initializing.
        /// </summary>
        [Obsolete("Override OnInitializedAsync")]
        protected virtual Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            Log.Info("Initializing async {0}.", this.DisplayName);
            return Task.FromResult(true);
        }

        /// <summary>
        /// Called when view has been initialized
        /// </summary>
        protected virtual Task OnInitializedAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// Called when activating.
        /// </summary>
        [Obsolete("Override OnActivatedAsync")]
        protected virtual Task OnActivateAsync(CancellationToken cancellationToken)
        {
            Log.Info("Task activate");
            return Task.FromResult(true);
        }


        /// <summary>
        /// Called when view has been activated.
        /// </summary>
        protected virtual Task OnActivatedAsync(CancellationToken cancellationToken)
        {
            Log.Info("Task activated");
            return Task.FromResult(true);
        }

        /// <summary>
        /// Called when deactivating.
        /// </summary>
        /// <param name = "close">Indicates whether this instance will be closed.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected virtual Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            Log.Info("Task deactivate");
            return Task.FromResult(true);
        }
    }
}
