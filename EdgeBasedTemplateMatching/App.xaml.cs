using System.Windows;

namespace EdgeBasedTemplateMatching
{
    using Prism.Ioc;
    using System.IO;
    using Views;

    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App
    {
        protected override Window CreateShell()
        {
            #region auto match system language

            string cultureName = System.Threading.Thread.CurrentThread.CurrentCulture.Name; // get current system language
            ResourceDictionary resourceDictionary = new ResourceDictionary();
            foreach (var item in Application.Current.Resources.MergedDictionaries)
            {
                string fileName = Path.GetFileNameWithoutExtension(item.Source.OriginalString);
                if (cultureName.ToLower().Contains(fileName.ToLower()))
                    resourceDictionary = item; // match language
            }
            if (resourceDictionary != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(resourceDictionary);
                Application.Current.Resources.MergedDictionaries.Add(resourceDictionary); // add language to mergedDictionaries last
            }

            #endregion auto match system language

            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }
    }
}