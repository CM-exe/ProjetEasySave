using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyRemote.Model;

namespace EasyRemote {
    public interface IViewModel {
        public IClientControler ClientControler { get; }
    }

    class ViewModel : IViewModel {
        public IClientControler ClientControler { get; } = ClientController.Instance;

    }
}
