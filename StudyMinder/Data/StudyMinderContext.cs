using Microsoft.EntityFrameworkCore;
using StudyMinder.Models;
using Microsoft.Data.Sqlite;
using System;

namespace StudyMinder.Data
{
    public class StudyMinderContext : DbContext
    {
        public StudyMinderContext()
        {
            Disciplinas = null!;
            Assuntos = null!;
            Estudos = null!;
            TiposEstudo = null!;
            Editais = null!;
            EditalAssuntos = null!;
            EditalCronograma = null!;
            Escolaridades = null!;
            StatusesEdital = null!;
            FasesEdital = null!;
            TiposProva = null!;
            Revisoes = null!;
            RevisoesCicloAtivo = null!;
            CicloEstudos = null!;
        }

        public StudyMinderContext(DbContextOptions<StudyMinderContext> options) : base(options)
        {
            // Garante que o banco de dados está criado
            Database.EnsureCreated();
        }

        public DbSet<Disciplina> Disciplinas { get; set; }
        public DbSet<Assunto> Assuntos { get; set; }
        public DbSet<Estudo> Estudos { get; set; }
        public DbSet<TipoEstudo> TiposEstudo { get; set; }
        public DbSet<Edital> Editais { get; set; }
        public DbSet<EditalAssunto> EditalAssuntos { get; set; }
        public DbSet<EditalCronograma> EditalCronograma { get; set; }
        public DbSet<Escolaridade> Escolaridades { get; set; }
        public DbSet<StatusEdital> StatusesEdital { get; set; }
        public DbSet<FaseEdital> FasesEdital { get; set; }
        public DbSet<TiposProva> TiposProva { get; set; }
        public DbSet<Revisao> Revisoes { get; set; }
        public DbSet<RevisaoCicloAtivo> RevisoesCicloAtivo { get; set; }
        public DbSet<CicloEstudo> CicloEstudos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Disciplina>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Cor).IsRequired().HasMaxLength(7);
                entity.Property(e => e.Arquivado).HasDefaultValue(false);
                entity.Property(e => e.DataCriacaoTicks).HasColumnName("DataCriacao");
                entity.Property(e => e.DataModificacaoTicks).HasColumnName("DataModificacao");
                entity.Ignore(e => e.DataCriacao);
                entity.Ignore(e => e.DataModificacao);
                entity.Ignore(e => e.Progresso);
                entity.Ignore(e => e.Rendimento);
                entity.Ignore(e => e.TotalAcertos);
                entity.Ignore(e => e.TotalErros);
                entity.Ignore(e => e.TotalAssuntos);
                entity.Ignore(e => e.HorasEstudadas);
                entity.Ignore(e => e.TotalQuestoes);
                entity.Ignore(e => e.TotalPaginasLidas);
            });

            modelBuilder.Entity<Assunto>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Concluido).HasDefaultValue(false);
                entity.Property(e => e.Arquivado).HasDefaultValue(false);
                entity.Property(e => e.DataCriacaoTicks).HasColumnName("DataCriacao");
                entity.Property(e => e.DataModificacaoTicks).HasColumnName("DataModificacao");
                entity.Ignore(e => e.DataCriacao);
                entity.Ignore(e => e.DataModificacao);
                entity.Ignore(e => e.HorasEstudadas);
                entity.Ignore(e => e.TotalAcertos);
                entity.Ignore(e => e.TotalErros);
                entity.Ignore(e => e.Rendimento);
                entity.Ignore(e => e.Progresso);
                entity.Ignore(e => e.IsEditing);
                
                entity.HasOne(e => e.Disciplina)
                      .WithMany(d => d.Assuntos)
                      .HasForeignKey(e => e.DisciplinaId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Estudo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Acertos).HasDefaultValue(0);
                entity.Property(e => e.Erros).HasDefaultValue(0);
                entity.Property(e => e.PaginaInicial).HasDefaultValue(0);
                entity.Property(e => e.PaginaFinal).HasDefaultValue(0);
                entity.Property(e => e.DataTicks).HasColumnName("Data");
                entity.Property(e => e.DataCriacaoTicks).HasColumnName("DataCriacao");
                entity.Property(e => e.DataModificacaoTicks).HasColumnName("DataModificacao");
                entity.Ignore(e => e.Data);
                entity.Ignore(e => e.DataCriacao);
                entity.Ignore(e => e.DataModificacao);
                entity.Ignore(e => e.Duracao);
                entity.Ignore(e => e.TotalQuestoes);
                entity.Ignore(e => e.RendimentoPercentual);
                entity.Ignore(e => e.TotalPaginas);
                entity.Ignore(e => e.HorasEstudadas);
                
                entity.HasOne(e => e.TipoEstudo)
                      .WithMany(t => t.Estudos)
                      .HasForeignKey(e => e.TipoEstudoId)
                      .OnDelete(DeleteBehavior.Restrict);
                      
                entity.HasOne(e => e.Assunto)
                      .WithMany(a => a.Estudos)
                      .HasForeignKey(e => e.AssuntoId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TipoEstudo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired();
                entity.Property(e => e.Ativo).HasDefaultValue(true);
                entity.Property(e => e.DataCriacaoTicks).HasColumnName("DataCriacao");
                entity.Ignore(e => e.DataCriacao);
                entity.Ignore(e => e.DataModificacao);
            });

            modelBuilder.Entity<Edital>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Cargo).IsRequired();
                entity.Property(e => e.Orgao).IsRequired();
                entity.Property(e => e.Salario).HasMaxLength(10);
                entity.Property(e => e.ValorInscricao).HasMaxLength(10);
                entity.Property(e => e.AcertosProva).HasDefaultValue(null);
                entity.Property(e => e.ErrosProva).HasDefaultValue(null);
                entity.Property(e => e.ProvaDiscursiva).HasDefaultValue(false);
                entity.Property(e => e.ProvaTitulos).HasDefaultValue(false);
                entity.Property(e => e.ProvaTaf).HasDefaultValue(false);
                entity.Property(e => e.ProvaPratica).HasDefaultValue(false);
                entity.Property(e => e.Arquivado).HasDefaultValue(false);
                entity.Property(e => e.DataAberturaTicks).HasColumnName("DataAbertura");
                entity.Property(e => e.DataProvaTicks).HasColumnName("DataProva");
                entity.Property(e => e.DataHomologacaoTicks).HasColumnName("DataHomologacao");
                entity.Property(e => e.DataCriacaoTicks).HasColumnName("DataCriacao");
                entity.Ignore(e => e.DataAbertura);
                entity.Ignore(e => e.DataProva);
                entity.Ignore(e => e.DataHomologacao);
                entity.Ignore(e => e.DataCriacao);
                entity.Ignore(e => e.DataModificacao);
                entity.Ignore(e => e.RendimentoProva);
                entity.Ignore(e => e.ProgressoGeral);
                entity.Ignore(e => e.TotalPaginasLidas);
                entity.Ignore(e => e.StatusDinamico);
                entity.Ignore(e => e.Encerrado);
                entity.Ignore(e => e.ValidadeFim);
                
                entity.HasOne(e => e.Escolaridade)
                      .WithMany(esc => esc.Editais)
                      .HasForeignKey(e => e.EscolaridadeId)
                      .OnDelete(DeleteBehavior.NoAction);
                      
                entity.HasOne(e => e.FaseEdital)
                      .WithMany(f => f.Editais)
                      .HasForeignKey(e => e.FaseEditalId)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<EditalAssunto>(entity =>
            {
                entity.HasKey(e => new { e.EditalId, e.AssuntoId });
                
                entity.HasOne(e => e.Edital)
                      .WithMany(ed => ed.EditalAssuntos)
                      .HasForeignKey(e => e.EditalId)
                      .OnDelete(DeleteBehavior.Cascade);
                      
                entity.HasOne(e => e.Assunto)
                      .WithMany(a => a.EditalAssuntos)
                      .HasForeignKey(e => e.AssuntoId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<EditalCronograma>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Evento).IsRequired();
                entity.Property(e => e.Concluido).HasDefaultValue(false);
                entity.Property(e => e.Ignorado).HasDefaultValue(false);
                entity.Property(e => e.DataEventoTicks).HasColumnName("DataEvento");
                entity.Property(e => e.DataCriacaoTicks).HasColumnName("DataCriacao");
                entity.Ignore(e => e.DataEvento);
                entity.Ignore(e => e.DataCriacao);
                entity.Ignore(e => e.DataModificacao);
                
                entity.HasOne(e => e.Edital)
                      .WithMany(ed => ed.EditalCronogramas)
                      .HasForeignKey(e => e.EditalId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Escolaridade>(entity =>
            {
                entity.HasKey(e => e.Id);
                // Nome is mapped to column "Escolaridade" via [Column] attribute
            });

            modelBuilder.Entity<StatusEdital>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(100);
            });

            modelBuilder.Entity<FaseEdital>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Fase).HasMaxLength(100);
            });

            modelBuilder.Entity<Revisao>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TipoRevisao).IsRequired().HasConversion<int>();
                entity.Property(e => e.DataProgramadaTicks).HasColumnName("DataProgramada");
                entity.Property(e => e.DataCriacaoTicks).HasColumnName("DataCriacao");
                entity.Property(e => e.DataModificacaoTicks).HasColumnName("DataModificacao");
                entity.Ignore(e => e.DataProgramada);
                entity.Ignore(e => e.DataCriacao);
                entity.Ignore(e => e.DataModificacao);
                entity.Ignore(e => e.EstaPendente);
                entity.Ignore(e => e.EstaConcluida);
                
                // Relacionamento EstudoOrigem (CASCADE - se estudo original for deletado, revisão também)
                entity.HasOne(e => e.EstudoOrigem)
                      .WithMany()
                      .HasForeignKey(e => e.EstudoOrigemId)
                      .OnDelete(DeleteBehavior.Cascade);
                      
                // Relacionamento EstudoRealizado (SET NULL - se estudo de revisão for deletado, revisão fica pendente)
                entity.HasOne(e => e.EstudoRealizado)
                      .WithMany()
                      .HasForeignKey(e => e.EstudoRealizadoId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<RevisaoCicloAtivo>(entity =>
            {
                entity.HasKey(e => e.AssuntoId);
                entity.Property(e => e.DataInclusaoTicks).HasColumnName("DataInclusao");
                entity.Ignore(e => e.DataInclusao);
                
                entity.HasOne(e => e.Assunto)
                      .WithMany()
                      .HasForeignKey(e => e.AssuntoId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CicloEstudo>(entity =>
            {
                // The table name and column names are defined by attributes in the model.
                // The primary key is AssuntoId, which is also the foreign key to Assunto,
                // creating a one-to-one relationship.
                entity.HasOne(e => e.Assunto)
                      .WithOne() // No navigation property on Assunto back to CicloEstudo
                      .HasForeignKey<CicloEstudo>(e => e.AssuntoId)
                      .OnDelete(DeleteBehavior.Cascade); // As per the CREATE TABLE statement
            });
            
        }
    }
}
