using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DialogWindow.Core
{
    public class UserViewModel : BaseViewModel
    {
        private string name;
        private int age;

        /// <summary>
        /// Gets or sets this user's name
        /// </summary>
        public string Name
        {
            get => this.name;
            // ref essentially gets a "pointer" to the field, meaning it can be
            // updated from anywhere without a reference to this UserViewModel instance
            set => RaisePropertyChanged(ref this.name, value);
        }

        /// <summary>
        /// Gets or sets this user's age
        /// </summary>
        public int Age
        {
            get => this.age;
            set => RaisePropertyChanged(ref this.age, value);
        }
    }
}
