using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Quetzal.Desktop.Sessao;

namespace Quetzal.Desktop.ApiClientes
{
    public abstract class ClienteHttp
    {
<<<<<<< HEAD
        // Base URL agora configurável via variável de ambiente QUETZAL_API_BASEURL
        // Ex.: https://seu-app-service.azurewebsites.net
        private static readonly string UrlBase =
            Environment.GetEnvironmentVariable("QUETZAL_API_BASEURL")
            ?? "http://localhost:5090";
=======
        private const string UrlBase = "https://app-quet-api-eadkehdndch5fmdz.brazilsouth-01.azurewebsites.net";
>>>>>>> 3389567ab67d36a62bdc222ec25e4c880aae4f64

        //private const string UrlBase = "http://localhost:5277";

        private static readonly HttpClient _httpClient = CriarHttpClient();

        private static HttpClient CriarHttpClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    (mensagem, cert, chain, erros) => true
            };

            var cliente = new HttpClient(handler)
            {
                BaseAddress = new Uri(UrlBase)
            };

            cliente.DefaultRequestHeaders.Accept.Clear();

            cliente.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            return cliente;
        }

        protected void AplicarAutenticacao()
        {
            var token = SessaoUsuario.Instancia.Token;

            if (!string.IsNullOrWhiteSpace(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        token);
            }
        }

        private StringContent SerializarCorpo(object corpo)
        {
            var json =
                JsonConvert.SerializeObject(corpo);

            return new StringContent(
                json,
                Encoding.UTF8,
                "application/json");
        }

        private async Task<TResposta> DesserializarRespostaAsync<TResposta>(
            HttpResponseMessage resposta)
        {
            var conteudo =
                await resposta.Content.ReadAsStringAsync();

            var resultado =
                JsonConvert.DeserializeObject<TResposta>(conteudo);

            if (resultado == null)
            {
                throw new Exception(
                    "Não foi possível interpretar a resposta da API.");
            }

            return resultado;
        }

        private async Task VerificarErroAsync(
            HttpResponseMessage resposta,
            string metodo,
            string rota)
        {
            if (resposta.IsSuccessStatusCode)
                return;

            var conteudo =
                await resposta.Content.ReadAsStringAsync();

            throw new Exception(
                $"A API retornou {(int)resposta.StatusCode} " +
                $"({resposta.ReasonPhrase}) " +
                $"na requisição {metodo} {rota}.\n\n" +
                $"Detalhes:\n{conteudo}");
        }

        protected async Task<TResposta> PostAsync<TResposta>(
            string rota,
            object corpo)
        {
            try
            {
                AplicarAutenticacao();

                var resposta =
                    await _httpClient.PostAsync(
                        rota,
                        SerializarCorpo(corpo));

                await VerificarErroAsync(
                    resposta,
                    "POST",
                    rota);

                return await DesserializarRespostaAsync<TResposta>(
                    resposta);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Erro ao comunicar com a API (POST {rota}): " +
                    $"{ex.Message}",
                    ex);
            }
        }

        protected async Task<TResposta> PostSemAutenticacaoAsync<TResposta>(
            string rota,
            object corpo)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;

                var resposta =
                    await _httpClient.PostAsync(
                        rota,
                        SerializarCorpo(corpo));

                await VerificarErroAsync(
                    resposta,
                    "POST",
                    rota);

                return await DesserializarRespostaAsync<TResposta>(
                    resposta);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Erro ao comunicar com a API " +
                    $"(POST público {rota}): {ex.Message}",
                    ex);
            }
        }

        protected async Task<TResposta> GetAsync<TResposta>(
            string rota)
        {
            try
            {
                AplicarAutenticacao();

                var resposta =
                    await _httpClient.GetAsync(rota);

                await VerificarErroAsync(
                    resposta,
                    "GET",
                    rota);

                return await DesserializarRespostaAsync<TResposta>(
                    resposta);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Erro ao comunicar com a API (GET {rota}): " +
                    $"{ex.Message}",
                    ex);
            }
        }

        protected async Task<TResposta> PutAsync<TResposta>(
            string rota,
            object corpo)
        {
            try
            {
                AplicarAutenticacao();

                var resposta =
                    await _httpClient.PutAsync(
                        rota,
                        SerializarCorpo(corpo));

                await VerificarErroAsync(
                    resposta,
                    "PUT",
                    rota);

                return await DesserializarRespostaAsync<TResposta>(
                    resposta);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Erro ao comunicar com a API (PUT {rota}): " +
                    $"{ex.Message}",
                    ex);
            }
        }

        protected async Task<TResposta> DeleteAsync<TResposta>(
            string rota)
        {
            try
            {
                AplicarAutenticacao();

                var resposta =
                    await _httpClient.DeleteAsync(rota);

                await VerificarErroAsync(
                    resposta,
                    "DELETE",
                    rota);

                return await DesserializarRespostaAsync<TResposta>(
                    resposta);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Erro ao comunicar com a API (DELETE {rota}): " +
                    $"{ex.Message}",
                    ex);
            }
        }

        protected async Task<TResposta> PostAsync<TResposta>(
            string rota,
            MultipartFormDataContent conteudo)
        {
            try
            {
                AplicarAutenticacao();

                var resposta =
                    await _httpClient.PostAsync(
                        rota,
                        conteudo);

                await VerificarErroAsync(
                    resposta,
                    "POST Multipart",
                    rota);

                return await DesserializarRespostaAsync<TResposta>(
                    resposta);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Erro ao comunicar com a API " +
                    $"(POST Multipart {rota}): {ex.Message}",
                    ex);
            }
        }
    }
}