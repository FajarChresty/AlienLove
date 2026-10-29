namespace AlienLove.Matchmaking
{
    /// <summary>
    /// Aturan "gagal berat / lawsuit". Syaratnya BELUM didefinisikan di GDD (open decision),
    /// jadi dipisah sebagai interface agar nanti tinggal diganti tanpa menyentuh controller.
    /// </summary>
    public interface ISevereFailureRule
    {
        bool IsSevere(CompatibilityResult result);
    }

    /// <summary>Default sementara: tidak ada gagal berat.</summary>
    public class NoSevereFailureRule : ISevereFailureRule
    {
        public bool IsSevere(CompatibilityResult result) { return false; }
    }
}
