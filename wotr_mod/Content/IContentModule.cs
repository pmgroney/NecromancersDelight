namespace wotr_mod.Content
{
    internal interface IContentModule
    {
        string Name { get; }
        void RegisterLocalization();
        void Install();
    }
}
