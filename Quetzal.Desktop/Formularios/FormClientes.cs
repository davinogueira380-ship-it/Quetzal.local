using Quetzal.Desktop.ApiClientes;

namespace Quetzal.Desktop.Formularios
{
    public partial class FormClientes : Form

    { // CONTROLLER
        private readonly UsuarioApiCliente _apiUsuario;
        //  DADOS DA TELA
        private List<UsuarioDto> _listaClientes = new List<UsuarioDto>();
        private string? _clienteSelecionadoId = null;
        // CONSTRUTOR
        public FormClientes()
        {
            InitializeComponent();
            _apiUsuario = new UsuarioApiCliente();
        }// CARREGAMENTO DO FORMULÁRIO
        private async void FormClientes_Load(object sender, EventArgs e)
        {
            await CarregarClientesAsync();

        }
        // CARREGAR CLIENTES
        private async Task CarregarClientesAsync()
        {
            try
            {
                dgvClientes.Enabled = false;
                _listaClientes = await _apiUsuario.ObterTodosAsync();
                AtualizarGrid(_listaClientes);
            }
            catch (Exception ex) { MessageBox.Show($"Não foi possível carregar a lista de clientes da API: {ex.Message}", "Aviso de Comunicação", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            finally { dgvClientes.Enabled = true; }
        } // ATUALIZAR GRID
        private void AtualizarGrid(List<UsuarioDto> dados)
        {
            dgvClientes.AutoGenerateColumns = false;
            dgvClientes.DataSource = null;
            dgvClientes.DataSource = dados;
            dgvClientes.ClearSelection();
        }
        // SELEÇÃO DE CLIENTE
        private void dgvClientes_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvClientes.SelectedRows.Count == 0)
                return;
            var linha = dgvClientes.SelectedRows[0];
            if (linha.DataBoundItem is UsuarioDto item)
            {
                _clienteSelecionadoId = item.Id;
                txtNomeCompleto.Text = item.NomeCompleto; txtEmail.Text = item.Email;
                txtTelefone.Text = item.Telefone;
                swPerfilAtivo.Checked = item.Ativo;
                btnAlternarAtivacao.Enabled = true;
                btnAlternarAtivacao.Text = item.Ativo ? " Desativar Perfil (Bloquear Acesso às Fotos)" : " Ativar Perfil (Liberar Acesso às Fotos)";
            }
        }
        // SALVAR ALTERAÇÕES 
        private async void btnSalvar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_clienteSelecionadoId))
            {
                MessageBox.Show("Selecione um cliente na tabela para editar.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var nome = txtNomeCompleto.Text.Trim();
            if (string.IsNullOrWhiteSpace(nome))
            {
                MessageBox.Show("O nome do cliente não pode ficar em branco.", "Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNomeCompleto.Focus();
                return;
            }
            try
            {
                btnSalvar.Enabled = false;
                // Busca o cliente que estava selecionado
                var clienteAtual = _listaClientes.FirstOrDefault(c => c.Id == _clienteSelecionadoId);

                // Cria um DTO com os dados atualizados e chama a API com a assinatura correta
                var dto = new UsuarioDto
                {
                    Id = _clienteSelecionadoId,
                    NomeCompleto = nome,
                    Email = txtEmail.Text.Trim(),
                    Telefone = txtTelefone.Text.Trim(),
                    Ativo = clienteAtual?.Ativo ?? false,
                    Perfis = clienteAtual?.Perfis?.ToList() ?? new List<string>()
                };

                await _apiUsuario.AtualizarAsync(_clienteSelecionadoId, dto);
                MessageBox.Show("Dados do cliente atualizados com sucesso!" + (swPerfilAtivo.Checked ? "\nO cliente agora tem permissão para visualizar as fotos na Área do Cliente." : "\nO acesso às fotos na Área do Cliente foi bloqueado."), "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information); await CarregarClientesAsync();
            }
            catch (Exception ex) { MessageBox.Show($"Erro ao atualizar cliente: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { btnSalvar.Enabled = true; }

        }  // ATIVAR / DESATIVAR PERFIL 
        private async void btnAlternarAtivacao_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_clienteSelecionadoId))
                return;
            var clienteAtual = _listaClientes.FirstOrDefault(c => c.Id == _clienteSelecionadoId);
            if (clienteAtual == null) return;
            var novoStatus = !clienteAtual.Ativo;
            var acaoTexto = novoStatus ? "ATIVAR o perfil e LIBERAR o acesso às fotos" : "DESATIVAR o perfil e BLOQUEAR o acesso às fotos";
            var confirmacao = MessageBox.Show($"Deseja realmente {acaoTexto} para o cliente '{clienteAtual.NomeCompleto}'?", "Confirmação de Permissão",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirmacao != DialogResult.Yes)
                return;
            try
            {
                if (novoStatus)
                {
                    await _apiUsuario.AtivarAsync(_clienteSelecionadoId);
                    MessageBox.Show("Perfil ativado! O cliente agora consegue visualizar as fotos na área do cliente.", "Perfil Ativado",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    await _apiUsuario.DesativarAsync(_clienteSelecionadoId);
                    MessageBox.Show("Perfil desativado! O cliente foi bloqueado de visualizar fotos até ser reativado.", "Perfil Desativado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                await CarregarClientesAsync();
            }
            catch (Exception ex) { MessageBox.Show($"Erro ao alternar status do cliente: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            //} 
            //// PESQUISA 
            // private void txtBusca_TextChanged( object sender, EventArgs e)

            //{ var filtrados = _apiUsuario.Filtrar(_listaClientes, txtBusca.Text);
            //    AtualizarGrid(filtrados);
            //} // ATUALIZAR LISTA
            // private async void btnAtualizar_Click( object sender, EventArgs e) { await CarregarClientesAsync(); 
            //}
        }
    }
}