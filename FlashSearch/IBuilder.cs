namespace FlashSearch
{
    public interface IBuilder<out T>
    {
        T Build();
    }
}