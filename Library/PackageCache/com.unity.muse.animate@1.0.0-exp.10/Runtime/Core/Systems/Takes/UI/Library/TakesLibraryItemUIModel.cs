namespace Unity.Muse.Animate
{
    class TakesLibraryItemUIModel : LibraryItemUIModel
    {
        TakeModel Take => Target as TakeModel;
        
        protected override void RegisterCallbacks()
        {
            base.RegisterCallbacks();
            Take.OnTakeChanged += OnTakeChanged;
        }
        
        protected override void UnregisterCallbacks()
        {
            Take.OnTakeChanged -= OnTakeChanged;
            base.UnregisterCallbacks();
        }
        
        void OnTakeChanged(TakeModel.TakeProperty takeProperty)
        {
            InvokeChanged();
        }
    }
}
