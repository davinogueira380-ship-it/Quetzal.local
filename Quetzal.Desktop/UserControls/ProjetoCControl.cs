using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Quetzal.Desktop.ApiClientes;

namespace Quetzal.Desktop.UserControls
{
    public partial class ProjetoCControl : System.Windows.Forms.UserControl
    {
        // ============================================================
        // API
        // ============================================================

        private readonly ProjetoCApiCliente _apiProjeto;
        private readonly UsuarioApiCliente _apiUsuario;


        // ============================================================
        // DADOS DA TELA
        // ============================================================

        private List<ProjetoCDto> _listaProjetos =
            new List<ProjetoCDto>();

        private List<UsuarioDto> _listaClientes =
            new List<UsuarioDto>();

        private List<FotoItemModel> _fotosDoProjeto =
            new List<FotoItemModel>();

        private int? _projetoSelecionadoId = null;

        private bool _dadosCarregados = false;


        // ============================================================
        // CLASSE PARA COMBOBOX
        // ============================================================

        public class ItemCombo
        {
            public string Id { get; set; } = string.Empty;

            public string Texto { get; set; } = string.Empty;

            public override string ToString() => Texto;
        }


        // ============================================================
        // MODELO INTERNO DAS FOTOS
        // ============================================================

        public class FotoItemModel
        {
            public string CaminhoArquivo { get; set; } = string.Empty;

            public string Base64 { get; set; } = string.Empty;

            public Image? Imagem { get; set; }

            public override string ToString()
            {
                if (!string.IsNullOrWhiteSpace(CaminhoArquivo))
                    return Path.GetFileName(CaminhoArquivo);

                return "Foto do Projeto";
            }
        }


        // ============================================================
        // CONSTRUTOR
        // ============================================================

        public ProjetoCControl()
        {
            InitializeComponent();

            _apiProjeto = new ProjetoCApiCliente();
            _apiUsuario = new UsuarioApiCliente();

            this.Load += ProjetoCControl_Load;
        }


        // ============================================================
        // LOAD
        // ============================================================

        private async void ProjetoCControl_Load(
            object sender,
            EventArgs e)
        {
            if (_dadosCarregados)
                return;

            _dadosCarregados = true;

            await CarregarDadosIniciaisAsync();
        }


        // ============================================================
        // CARREGAMENTO INICIAL
        // ============================================================

        private async Task CarregarDadosIniciaisAsync()
        {
            try
            {
                // ----------------------------------------------------
                // CLIENTES
                // ----------------------------------------------------

                _listaClientes =
                    await _apiUsuario.ObterTodosAsync();

                cmbCliente.Items.Clear();

                cmbCliente.Items.Add(
                    new ItemCombo
                    {
                        Id = "",
                        Texto = "-- Selecione o Cliente --"
                    });

                foreach (var cliente in _listaClientes)
                {
                    var status =
                        cliente.Ativo
                            ? ""
                            : " (Inativo)";

                    cmbCliente.Items.Add(
                        new ItemCombo
                        {
                            Id = cliente.Id,
                            Texto =
                                $"{cliente.NomeCompleto}{status}"
                        });
                }

                if (cmbCliente.Items.Count > 0)
                    cmbCliente.SelectedIndex = 0;


                // ----------------------------------------------------
                // PROJETOS
                // ----------------------------------------------------

                await CarregarProjetosAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao carregar dados do formulário: {ex.Message}",
                    "Aviso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }


        // ============================================================
        // CARREGAR PROJETOS
        // ============================================================

        private async Task CarregarProjetosAsync()
        {
            try
            {
                dgvProjetos.Enabled = false;

                _listaProjetos =
                    await _apiProjeto.ObterTodosAsync(
                        incluirInativos: true);

                AtualizarGrid(_listaProjetos);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Não foi possível carregar a lista de projetos: {ex.Message}",
                    "Aviso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                dgvProjetos.Enabled = true;
            }
        }


        // ============================================================
        // ATUALIZAR GRID
        // ============================================================

        private void AtualizarGrid(
            List<ProjetoCDto> dados)
        {
            dgvProjetos.AutoGenerateColumns = false;

            dgvProjetos.DataSource = null;

            dgvProjetos.DataSource = dados;

            dgvProjetos.ClearSelection();
        }


        // ============================================================
        // SELEÇÃO DO PROJETO
        // ============================================================

        private void dgvProjetos_SelectionChanged(
            object sender,
            EventArgs e)
        {
            if (dgvProjetos.SelectedRows.Count == 0)
                return;

            var linha =
                dgvProjetos.SelectedRows[0];

            if (linha.DataBoundItem is not ProjetoCDto item)
                return;

            _projetoSelecionadoId = item.Id;

            txtNomeProjeto.Text = item.Nome;

            txtDescricao.Text = item.Descricao;

            swAtivo.Checked = item.Ativo;


            // --------------------------------------------------------
            // CLIENTE
            // --------------------------------------------------------

            for (int i = 0; i < cmbCliente.Items.Count; i++)
            {
                if (cmbCliente.Items[i] is ItemCombo combo &&
                    combo.Id == item.UsuarioId)
                {
                    cmbCliente.SelectedIndex = i;

                    break;
                }
            }


            // --------------------------------------------------------
            // FOTOS
            // --------------------------------------------------------

            _fotosDoProjeto.Clear();

            if (item.Fotos != null)
            {
                foreach (var fotoBase64 in item.Fotos)
                {
                    if (string.IsNullOrWhiteSpace(fotoBase64))
                        continue;

                    var foto = new FotoItemModel
                    {
                        Base64 = fotoBase64,
                        CaminhoArquivo = string.Empty
                    };

                    try
                    {
                        var bytes =
                            Convert.FromBase64String(fotoBase64);

                        using var ms =
                            new MemoryStream(bytes);

                        using var imagemTemporaria =
                            Image.FromStream(ms);

                        foto.Imagem =
                            new Bitmap(imagemTemporaria);
                    }
                    catch
                    {
                        foto.Imagem = null;
                    }

                    _fotosDoProjeto.Add(foto);
                }
            }

            AtualizarListaFotos();

            picPreviewFoto.Image = null;


            // --------------------------------------------------------
            // ATIVAR / DESATIVAR
            // --------------------------------------------------------

            btnDesativar.Enabled = true;
            btnExcluir.Enabled = true;

            btnDesativar.Text =
                item.Ativo
                    ? "🗑️ Desativar Projeto"
                    : "♻️ Reativar Projeto";
        }

        // ============================================================
        // NOVO
        // ============================================================

        private void btnNovo_Click(
            object sender,
            EventArgs e)
        {
            LimparCampos();
        }


        // ============================================================
        // LIMPAR CAMPOS
        // ============================================================

        private void LimparCampos()
        {
            _projetoSelecionadoId = null;

            txtNomeProjeto.Clear();

            txtDescricao.Clear();

            if (cmbCliente.Items.Count > 0)
                cmbCliente.SelectedIndex = 0;

            swAtivo.Checked = true;

            _fotosDoProjeto.Clear();

            AtualizarListaFotos();

            picPreviewFoto.Image = null;

            dgvProjetos.ClearSelection();

            //btnDesativar.Enabled = false;
            //btnExcluir.Enabled = false;
            btnDesativar.Text =
                "Desativar Projeto";

            txtNomeProjeto.Focus();
        }


        // ============================================================
        // ADICIONAR FOTO
        // ============================================================

        private void btnAdicionarFoto_Click(
            object sender,
            EventArgs e)
        {
            using var ofd =
                new OpenFileDialog
                {
                    Title =
                        "Selecionar Foto do Projeto",

                    Filter =
                        "Arquivos de Imagem|" +
                        "*.jpg;*.jpeg;*.png;*.webp;*.bmp|" +
                        "Todos os Arquivos|*.*",

                    Multiselect = true
                };

            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                foreach (var arquivo in ofd.FileNames)
                {
                    var bytes =
                        File.ReadAllBytes(arquivo);

                    var base64 =
                        Convert.ToBase64String(bytes);

                    using var ms =
                        new MemoryStream(bytes);

                    using var imagemTemporaria =
                        Image.FromStream(ms);

                    var foto =
                        new FotoItemModel
                        {
                            CaminhoArquivo = arquivo,

                            Base64 = base64,

                            Imagem =
                                new Bitmap(imagemTemporaria)
                        };

                    _fotosDoProjeto.Add(foto);
                }

                AtualizarListaFotos();

                if (_fotosDoProjeto.Count > 0)
                {
                    int ultimoIndice = _fotosDoProjeto.Count - 1;

                    lstFotos.SelectedIndex = ultimoIndice;

                    var fotoSelecionada = _fotosDoProjeto[ultimoIndice];

                    if (fotoSelecionada.Imagem != null)
                    {
                        picPreviewFoto.Image = fotoSelecionada.Imagem;
                        picPreviewFoto.SizeMode = PictureBoxSizeMode.Zoom;
                        picPreviewFoto.Refresh();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao carregar imagem: {ex.Message}",
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }


        // ============================================================
        // ATUALIZAR LISTA DE FOTOS
        // ============================================================

        private void AtualizarListaFotos()
        {
            lstFotos.Items.Clear();

            for (int i = 0; i < _fotosDoProjeto.Count; i++)
            {
                lstFotos.Items.Add($"Foto {i + 1}");
            }
        }


        // ============================================================
        // PREVIEW DA FOTO
        // ============================================================

        private void lstFotos_SelectedIndexChanged(
            object sender,
            EventArgs e)
        {
            var indice =
                lstFotos.SelectedIndex;

            if (indice < 0 ||
                indice >= _fotosDoProjeto.Count)
            {
                picPreviewFoto.Image = null;

                return;
            }

            var foto =
                _fotosDoProjeto[indice];

            if (foto.Imagem != null)
            {
                picPreviewFoto.Image =
                    foto.Imagem;

                return;
            }

            if (!string.IsNullOrWhiteSpace(foto.Base64))
            {
                try
                {
                    var bytes =
                        Convert.FromBase64String(
                            foto.Base64);

                    using var ms =
                        new MemoryStream(bytes);

                    using var imagemTemporaria =
                        Image.FromStream(ms);

                    foto.Imagem =
                        new Bitmap(imagemTemporaria);

                    picPreviewFoto.Image =
                        foto.Imagem;
                }
                catch
                {
                    picPreviewFoto.Image = null;
                }
            }
        }


        // ============================================================
        // REMOVER FOTO
        // ============================================================

        private void btnRemoverFoto_Click(
            object sender,
            EventArgs e)
        {
            var indice =
                lstFotos.SelectedIndex;

            if (indice < 0 ||
                indice >= _fotosDoProjeto.Count)
            {
                MessageBox.Show(
                    "Selecione uma foto para remover.",
                    "Atenção",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            var confirmacao =
                MessageBox.Show(
                    "Deseja remover esta foto do projeto?",
                    "Remover Foto",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

            if (confirmacao != DialogResult.Yes)
                return;

            _fotosDoProjeto.RemoveAt(indice);

            picPreviewFoto.Image = null;

            AtualizarListaFotos();
        }


        // ============================================================
        // SALVAR
        // ============================================================

        private async void btnSalvar_Click(
            object sender,
            EventArgs e)
        {
            var nome =
                txtNomeProjeto.Text.Trim();

            var descricao =
                txtDescricao.Text.Trim();


            // --------------------------------------------------------
            // NOME
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(nome))
            {
                MessageBox.Show(
                    "Informe o nome do projeto.",
                    "Campo Obrigatório",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txtNomeProjeto.Focus();

                return;
            }


            // --------------------------------------------------------
            // DESCRIÇÃO
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(descricao))
            {
                MessageBox.Show(
                    "Informe a descrição do projeto.",
                    "Campo Obrigatório",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txtDescricao.Focus();

                return;
            }


            // --------------------------------------------------------
            // CLIENTE
            // --------------------------------------------------------

            var clienteCombo =
                cmbCliente.SelectedItem as ItemCombo;

            var clienteId =
                clienteCombo?.Id ?? string.Empty;

            if (string.IsNullOrWhiteSpace(clienteId))
            {
                MessageBox.Show(
                    "Selecione a qual cliente este projeto pertence.",
                    "Campo Obrigatório",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                cmbCliente.Focus();

                return;
            }


            // --------------------------------------------------------
            // FOTOS
            // --------------------------------------------------------

            var fotos =
                _fotosDoProjeto
                    .Where(f =>
                        !string.IsNullOrWhiteSpace(
                            f.Base64))
                    .Select(f => f.Base64)
                    .ToList();


            try
            {
                btnSalvar.Enabled = false;


                // ----------------------------------------------------
                // DTO
                // ----------------------------------------------------

                var dto =
                    new ProjetoCDto
                    {
                        Id =
                            _projetoSelecionadoId ?? 0,

                        Nome =
                            nome,

                        Descricao =
                            descricao,

                        UsuarioId =
                            clienteId,

                        UsuarioNome =
                            clienteCombo?.Texto ?? "",

                        Fotos =
                            fotos,

                        Ativo =
                            swAtivo.Checked,

                        DataCadastro =
                            DateTime.Now
                    };


                // ----------------------------------------------------
                // NOVO PROJETO
                // ----------------------------------------------------

                if (_projetoSelecionadoId == null ||
                    _projetoSelecionadoId == 0)
                {
                    await _apiProjeto
                        .CadastrarAsync(dto);

                    MessageBox.Show(
                        "Projeto cadastrado com sucesso!",
                        "Sucesso",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                // ----------------------------------------------------
                // ATUALIZAÇÃO
                // ----------------------------------------------------

                else
                {
                    await _apiProjeto
                        .AtualizarAsync(
                            _projetoSelecionadoId.Value,
                            dto);

                    MessageBox.Show(
                        "Projeto atualizado com sucesso!",
                        "Sucesso",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }


                await CarregarProjetosAsync();

                LimparCampos();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao salvar projeto: {ex.Message}",
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnSalvar.Enabled = true;
            }
        }


        // ============================================================
        // DESATIVAR / REATIVAR
        // ============================================================

        private async void btnDesativar_Click(
            object sender,
            EventArgs e)
        {
            if (_projetoSelecionadoId == null)
            {
                MessageBox.Show(
                    "Selecione um projeto.",
                    "Atenção",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            var projetoAtual =
                _listaProjetos
                    .FirstOrDefault(
                        p =>
                            p.Id ==
                            _projetoSelecionadoId.Value);

            if (projetoAtual == null)
                return;


            var acao =
                projetoAtual.Ativo
                    ? "desativar"
                    : "reativar";


            var confirmacao =
                MessageBox.Show(
                    $"Deseja realmente {acao} o projeto " +
                    $"'{projetoAtual.Nome}'?",
                    "Confirmação",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);


            if (confirmacao != DialogResult.Yes)
                return;


            try
            {
                btnDesativar.Enabled = false;


                // ----------------------------------------------------
                // DESATIVAR
                // ----------------------------------------------------

                if (projetoAtual.Ativo)
                {
                    await _apiProjeto
                        .DesativarAsync(
                            _projetoSelecionadoId.Value);

                    MessageBox.Show(
                        "Projeto desativado com sucesso!",
                        "Sucesso",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                // ----------------------------------------------------
                // REATIVAR
                // ----------------------------------------------------

                else
                {
                    await _apiProjeto
                        .ReativarAsync(
                            _projetoSelecionadoId.Value);

                    MessageBox.Show(
                        "Projeto reativado com sucesso!",
                        "Sucesso",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }


                await CarregarProjetosAsync();

                LimparCampos();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao alterar situação do projeto: {ex.Message}",
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnDesativar.Enabled =
                    _projetoSelecionadoId != null;
            }
        }


        // ============================================================
        // PESQUISA
        // ============================================================

        private void txtBusca_TextChanged(
            object sender,
            EventArgs e)
        {
            var termo =
                txtBusca.Text.Trim();

            if (string.IsNullOrWhiteSpace(termo))
            {
                AtualizarGrid(_listaProjetos);

                return;
            }

            var filtrados =
                _listaProjetos
                    .Where(p =>
                        (p.Nome?.Contains(
                            termo,
                            StringComparison.OrdinalIgnoreCase)
                            ?? false)

                        ||

                        (p.UsuarioNome?.Contains(
                            termo,
                            StringComparison.OrdinalIgnoreCase)
                            ?? false)

                        ||

                        (p.Descricao?.Contains(
                            termo,
                            StringComparison.OrdinalIgnoreCase)
                            ?? false))
                    .ToList();

            AtualizarGrid(filtrados);
        }


        // ============================================================
        // ATUALIZAR LISTA
        // ============================================================

        private async void btnAtualizar_Click(
            object sender,
            EventArgs e)
        {
            await CarregarProjetosAsync();
        }

        // ============================================================
        // EXCLUIR PERMANENTEMENTE
        // ============================================================

        private async void btnExcluir_Click(object sender, EventArgs e)
        {
            // --------------------------------------------------------
            // VERIFICA SE EXISTE PROJETO SELECIONADO
            // --------------------------------------------------------

            if (_projetoSelecionadoId == null)
            {
                MessageBox.Show(
                    "Selecione um projeto para excluir.",
                    "Atenção",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }


            // --------------------------------------------------------
            // LOCALIZA O PROJETO SELECIONADO
            // --------------------------------------------------------

            var projetoSelecionado =
                _listaProjetos.FirstOrDefault(
                    p => p.Id == _projetoSelecionadoId.Value);

            if (projetoSelecionado == null)
            {
                MessageBox.Show(
                    "Não foi possível localizar o projeto selecionado.",
                    "Atenção",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }


            // --------------------------------------------------------
            // PRIMEIRA CONFIRMAÇÃO
            // --------------------------------------------------------

            var confirmacao =
                MessageBox.Show(
                    $"Deseja realmente excluir permanentemente o projeto " +
                    $"'{projetoSelecionado.Nome}'?\n\n" +
                    "Esta ação não poderá ser desfeita.",
                    "Excluir Projeto Permanentemente",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

            if (confirmacao != DialogResult.Yes)
                return;


            // --------------------------------------------------------
            // SEGUNDA CONFIRMAÇÃO
            // --------------------------------------------------------

            var confirmacaoFinal =
                MessageBox.Show(
                    $"ATENÇÃO!\n\n" +
                    $"O projeto '{projetoSelecionado.Nome}' será removido " +
                    $"permanentemente do sistema.\n\n" +
                    $"Deseja continuar?",
                    "Confirmação de Exclusão",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

            if (confirmacaoFinal != DialogResult.Yes)
                return;


            try
            {
                btnExcluir.Enabled = false;


                // ----------------------------------------------------
                // CHAMA A API
                // ----------------------------------------------------

                await _apiProjeto.ExcluirPermanentementeAsync(
                    _projetoSelecionadoId.Value);


                // ----------------------------------------------------
                // SUCESSO
                // ----------------------------------------------------

                MessageBox.Show(
                    "Projeto excluído permanentemente com sucesso!",
                    "Sucesso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);


                // ----------------------------------------------------
                // ATUALIZA A LISTA
                // ----------------------------------------------------

                await CarregarProjetosAsync();

                LimparCampos();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao excluir permanentemente o projeto: {ex.Message}",
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnExcluir.Enabled =
                    _projetoSelecionadoId != null;
            }
        }
    }

}