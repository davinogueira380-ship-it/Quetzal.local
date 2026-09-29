using SkiaSharp;

namespace Quetzal.Application.Utilitarios
{
    public static class ProcessadorImagem
    {
        // Maior dimensão permitida para a imagem.
        private const int TamanhoMaximo = 1600;

        // Qualidade do JPEG: 1 a 100.
        private const int QualidadeJpeg = 75;

        public static string ReduzirImagemBase64(string imagemBase64)
        {
            if (string.IsNullOrWhiteSpace(imagemBase64))
                return imagemBase64;

            try
            {
                // Remove o prefixo data:image/...;base64, se existir.
                string base64Limpo = imagemBase64;

                int indiceVirgula = imagemBase64.IndexOf(',');

                if (imagemBase64.StartsWith(
                        "data:image",
                        StringComparison.OrdinalIgnoreCase)
                    && indiceVirgula >= 0)
                {
                    base64Limpo = imagemBase64[(indiceVirgula + 1)..];
                }

                // Converte Base64 para bytes.
                byte[] bytesImagem =
                    Convert.FromBase64String(base64Limpo);

                // Decodifica a imagem.
                using SKBitmap imagemOriginal =
                    SKBitmap.Decode(bytesImagem)
                    ?? throw new InvalidOperationException(
                        "Não foi possível interpretar a imagem.");

                int largura = imagemOriginal.Width;
                int altura = imagemOriginal.Height;

                SKBitmap imagemFinal = imagemOriginal;
                SKBitmap? imagemRedimensionada = null;

                // Redimensiona somente se ultrapassar 1600 px.
                if (largura > TamanhoMaximo ||
                    altura > TamanhoMaximo)
                {
                    double proporcao = Math.Min(
                        (double)TamanhoMaximo / largura,
                        (double)TamanhoMaximo / altura);

                    int novaLargura =
                        (int)Math.Round(largura * proporcao);

                    int novaAltura =
                        (int)Math.Round(altura * proporcao);

                    imagemRedimensionada =
                        imagemOriginal.Resize(
                            new SKImageInfo(
                                novaLargura,
                                novaAltura),
                            SKSamplingOptions.Default);

                    if (imagemRedimensionada == null)
                    {
                        throw new InvalidOperationException(
                            "Não foi possível redimensionar a imagem.");
                    }

                    imagemFinal = imagemRedimensionada;
                }

                try
                {
                    // Converte/comprime para JPEG.
                    using SKImage imagem =
                        SKImage.FromBitmap(imagemFinal);

                    using SKData dados =
                        imagem.Encode(
                            SKEncodedImageFormat.Jpeg,
                            QualidadeJpeg);

                    if (dados == null)
                    {
                        throw new InvalidOperationException(
                            "Não foi possível comprimir a imagem.");
                    }

                    byte[] imagemReduzida =
                        dados.ToArray();

                    return Convert.ToBase64String(
                        imagemReduzida);
                }
                finally
                {
                    imagemRedimensionada?.Dispose();
                }
            }
            catch (FormatException)
            {
                throw new InvalidOperationException(
                    "A imagem recebida não possui um Base64 válido.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Não foi possível processar a imagem: {ex.Message}",
                    ex);
            }
        }
    }
}