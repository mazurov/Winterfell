using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace Bran
{
    [RunInstaller(true)]
    public partial class BranInstaller : System.Configuration.Install.Installer
    {
        private ServiceInstaller _serviceInstaller;
        private ServiceProcessInstaller _processInstaller;
        
        public BranInstaller()
        {
            InitializeComponent();
            _processInstaller = new ServiceProcessInstaller { Account = ServiceAccount.LocalSystem };
            _serviceInstaller = new ServiceInstaller {StartType = ServiceStartMode.Automatic, ServiceName = "Bran"};

            Installers.Add(_serviceInstaller);
            Installers.Add(_processInstaller);
        }
        
    }
}
