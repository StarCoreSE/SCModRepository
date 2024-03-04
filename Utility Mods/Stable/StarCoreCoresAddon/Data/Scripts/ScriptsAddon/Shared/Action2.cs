using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MIG.Shared.CSharp {
    public interface Action2<T, K> {
        void run(T t, K k);
    }

     public interface Action1<T> {
        void run(T t);
    }
}
