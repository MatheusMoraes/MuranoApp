using MuranoApp.Options;

namespace MuranoApp
{
    public interface IApiBlockStateStore
    {
        ApiBlockState Get();
        void Set(ApiBlockState state);
    }
}
