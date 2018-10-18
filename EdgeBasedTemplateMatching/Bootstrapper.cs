using System.Windows;

namespace EdgeBasedTemplateMatching
{
    using System;
    using System.IO;
    using Microsoft.Practices.Unity;
    using Prism.Modularity;
    using Prism.Unity;
    using Views;

    internal class Bootstrapper : UnityBootstrapper
    {
        protected override DependencyObject CreateShell()
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

        protected override void InitializeShell()
        {
            Application.Current.MainWindow.Show();
        }

        protected override void ConfigureModuleCatalog()
        {
            var moduleCatalog = (ModuleCatalog)ModuleCatalog;
            //moduleCatalog.AddModule(typeof(YOUR_MODULE));
        }
    }
}