
using System;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using Quetzal.Desktop.Sessao;
using Quetzal.Desktop.UserControls;

namespace Quetzal.Desktop
{
    public partial class FormPrincipal : Form
    {
        private System.Windows.Forms.UserControl? _userControlAtivo = null;

        public FormPrincipal()
        {
            InitializeComponent();
        }

        private void FormPrincipal_Load(object sender, EventArgs e)
        {
            // Atualiza informações do usuário logado
            var sessao = SessaoUsuario.Instancia;

            if (!string.IsNullOrWhiteSpace(sessao.NomeUsuario))
            {
                lblUsuarioLogado.Text =
                    $"👤 {sessao.NomeUsuario} | Designer Admin";
            }

            // Abre Ambiente como tela inicial
            btnAmbientes.Checked = true;
            btnAmbientes_Click(btnAmbientes, EventArgs.Empty);
        }

        /// <summary>
        /// Exibe um UserControl dentro do painel principal.
        /// </summary>
        private void AbrirUserControl(
            System.Windows.Forms.UserControl userControl,
            string titulo,
            Guna2Button botaoMenu)
        {
            // Remove o UserControl atualmente aberto
            if (_userControlAtivo != null)
            {
                pnlConteudo.Controls.Remove(_userControlAtivo);
                _userControlAtivo.Dispose();
                _userControlAtivo = null;
            }

            // Desmarca todos os botões
            DesmarcarBotoesMenu();

            // Marca o botão selecionado
            botaoMenu.Checked = true;

            // Atualiza o título do módulo
            lblTituloModulo.Text = titulo;

            // Guarda o UserControl atual
            _userControlAtivo = userControl;

            // Faz o UserControl ocupar todo o painel
            userControl.Dock = DockStyle.Fill;

            // Adiciona o UserControl ao painel
            pnlConteudo.Controls.Clear();
            pnlConteudo.Controls.Add(userControl);

            // Coloca na frente
            userControl.BringToFront();
        }

        /// <summary>
        /// Desmarca os botões do menu.
        /// </summary>
        private void DesmarcarBotoesMenu()
        {
            btnAmbientes.Checked = false;
            btnClientes.Checked = false;
            btnProjetos.Checked = false;
            btnPortfolio.Checked = false;
        }

        private void btnAmbientes_Click(object sender, EventArgs e)
        {
            AbrirUserControl(
                new AmbientesControl(),
                "🛋️ Gerenciamento de Ambientes",
                btnAmbientes
            );
        }

        private void btnClientes_Click(object sender, EventArgs e)
        {
            AbrirUserControl(
                new ClientesControl(),
                " Gestão de Clientes e Ativação de Acesso",
                btnClientes
            );
        }

        private void btnProjetos_Click(object sender, EventArgs e)
        {
            AbrirUserControl(
                 new Quetzal.Desktop.UserControls.ProjetoCControl(),
                     " Projetos de Clientes e Galeria por Ambiente",
                btnProjetos
                 );

        }

        private void btnPortfolio_Click(object sender, EventArgs e)
        {
            AbrirUserControl(
                new PortfolioControl(),
                " Portfólio Público para o Site",
                btnPortfolio
            );
        }

        private void btnSair_Click(object sender, EventArgs e)
        {
            var confirmacao = MessageBox.Show(
                "Deseja realmente sair do painel administrativo?",
                "Confirmar Saída",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirmacao == DialogResult.Yes)
            {
                SessaoUsuario.Instancia.Limpar();
                Close();
            }
        }

        private void pnlConteudo_Paint(object sender, PaintEventArgs e)
        {
        }
    }
}