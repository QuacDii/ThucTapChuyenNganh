using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace TTCN.Models
{
    public partial class QLDVContext : DbContext
    {
        public QLDVContext()
        {
        }

        public QLDVContext(DbContextOptions<QLDVContext> options)
            : base(options)
        {
        }

        public virtual DbSet<ChiTietScGn> ChiTietScGnVes { get; set; } = null!;
        public virtual DbSet<CumRap> CumRaps { get; set; } = null!;
        public virtual DbSet<DoAn> DoAns { get; set; } = null!;
        public virtual DbSet<DonDatVe> DonDatVes { get; set; } = null!;
        public virtual DbSet<DonDatVeDoAn> DonDatVeDoAns { get; set; } = null!;
        public virtual DbSet<GheNgoi> GheNgois { get; set; } = null!;
        public virtual DbSet<Phim> Phims { get; set; } = null!;
        public virtual DbSet<PhimTheLoai> PhimTheLoais { get; set; } = null!;
        public virtual DbSet<PhongChieu> PhongChieus { get; set; } = null!;
        public virtual DbSet<SuatChieu> SuatChieus { get; set; } = null!;
        public virtual DbSet<TheLoai> TheLoais { get; set; } = null!;
        public virtual DbSet<User> Users { get; set; } = null!;
        public virtual DbSet<UsersPhim> UsersPhims { get; set; } = null!;
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseSqlServer("Server=localhost;Database=QLDV;Trusted_Connection=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
             modelBuilder.Entity<ChiTietScGn>(entity =>
            {
                entity.HasKey(e => e.MaCt);

                entity.ToTable("ChiTiet_SC_GN_Ve");

                entity.Property(e => e.MaCt)
                    .ValueGeneratedNever()
                    .HasColumnName("maCT");

                entity.Property(e => e.MaGhe).HasColumnName("maGhe");

                entity.Property(e => e.MaSuat).HasColumnName("maSuat");

                entity.Property(e => e.TrangThai).HasColumnName("trangThai");

                entity.HasOne(d => d.MaGheNavigation)
                    .WithMany(p => p.ChiTietScGn)
                    .HasForeignKey(d => d.MaGhe)
                    .HasConstraintName("FK_ChiTiet_SC_GN_Ve_gheNgoi");

                entity.HasOne(d => d.MaSuatNavigation)
                    .WithMany(p => p.ChiTietScGn)
                    .HasForeignKey(d => d.MaSuat)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ChiTiet_SC_GN_Ve_suatChieu");
            });
            modelBuilder.Entity<CumRap>(entity =>
            {
                entity.HasKey(e => e.MaCumRap)
                    .HasName("PK__cumRap__988740F1A1C7DB6D");

                entity.ToTable("cumRap");

                entity.Property(e => e.MaCumRap)
                    .ValueGeneratedNever()
                    .HasColumnName("maCumRap");

                entity.Property(e => e.DiaChi)
                    .HasMaxLength(255)
                    .HasColumnName("diaChi");

                entity.Property(e => e.TenCumRap)
                    .HasMaxLength(100)
                    .HasColumnName("tenCumRap");

                entity.Property(e => e.ThanhPho)
                    .HasMaxLength(50)
                    .HasColumnName("thanhPho");
            });

            modelBuilder.Entity<DoAn>(entity =>
            {
                entity.HasKey(e => e.MaCombo)
                    .HasName("PK__doAn__CF0375EC9D3FD968");

                entity.ToTable("doAn");

                entity.Property(e => e.MaCombo)
                    .ValueGeneratedNever()
                    .HasColumnName("maCombo");

                entity.Property(e => e.MoTa)
                    .HasMaxLength(255)
                    .HasColumnName("moTa");
            });

            modelBuilder.Entity<DonDatVe>(entity =>
            {
                entity.HasKey(e => e.MaDon)
                    .HasName("PK__donDatVe__2431086D439285FE");

                entity.ToTable("donDatVe");

                entity.Property(e => e.MaDon)
                    .ValueGeneratedNever()
                    .HasColumnName("maDon");

                entity.Property(e => e.MaSuat).HasColumnName("maSuat");

                entity.Property(e => e.NgayDat)
                    .HasColumnType("datetime")
                    .HasColumnName("ngayDat");

                entity.Property(e => e.TongTien)
                    .HasColumnType("money")
                    .HasColumnName("tongTien");

                entity.Property(e => e.TrangThai)
                    .HasMaxLength(20)
                    .HasColumnName("trangThai");

                entity.HasOne(d => d.MaSuatNavigation)
                    .WithMany(p => p.DonDatVes)
                    .HasForeignKey(d => d.MaSuat)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_donDatVe_suatChieu");
            });

            modelBuilder.Entity<DonDatVeDoAn>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("donDatVe_doAn");

                entity.Property(e => e.MaCombo).HasColumnName("maCombo");

                entity.Property(e => e.MaDon).HasColumnName("maDon");

                entity.HasOne(d => d.MaComboNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.MaCombo)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_donDatVe_doAn_doAn");

                entity.HasOne(d => d.MaDonNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.MaDon)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_donDatVe_doAn_donDatVe");
            });

            modelBuilder.Entity<GheNgoi>(entity =>
            {
                entity.HasKey(e => e.MaGhe)
                    .HasName("PK__gheNgoi__2D87404CE1D3E68B");

                entity.ToTable("gheNgoi");

                entity.Property(e => e.MaGhe)
                    .ValueGeneratedNever()
                    .HasColumnName("maGhe");

                entity.Property(e => e.HangGhe)
                    .HasMaxLength(1)
                    .IsUnicode(false)
                    .HasColumnName("hangGhe")
                    .IsFixedLength();

                entity.Property(e => e.LoaiGhe)
                    .HasMaxLength(20)
                    .HasColumnName("loaiGhe");

                entity.Property(e => e.MaPhong).HasColumnName("maPhong");
               
                entity.Property(e => e.TenGhe).HasColumnName("tenGhe");

                entity.HasOne(d => d.MaPhongNavigation)
                    .WithMany(p => p.GheNgois)
                    .HasForeignKey(d => d.MaPhong)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_gheNgoi_phongChieu");
            });

            modelBuilder.Entity<Phim>(entity =>
            {
                entity.HasKey(e => e.MaPhim)
                    .HasName("PK__Phim__9F38F630C7CE2A4C");

                entity.ToTable("Phim");

                entity.Property(e => e.MaPhim)
                    .ValueGeneratedNever()
                    .HasColumnName("maPhim");

                entity.Property(e => e.DaoDien)
                    .HasMaxLength(50)
                    .HasColumnName("daoDien");

                entity.Property(e => e.MoTa)
                    .HasMaxLength(255)
                    .HasColumnName("moTa");

                entity.Property(e => e.NgayKetThuc)
                    .HasColumnType("date")
                    .HasColumnName("ngayKetThuc");

                entity.Property(e => e.NgayPhatHanh)
                    .HasColumnType("date")
                    .HasColumnName("ngayPhatHanh");

                entity.Property(e => e.PosterPhim)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("posterPhim");

                entity.Property(e => e.TenPhim)
                    .HasMaxLength(100)
                    .HasColumnName("tenPhim");

                entity.Property(e => e.ThoiLuong).HasColumnName("thoiLuong");

                entity.Property(e => e.TrailerPhim)
                    .HasMaxLength(150)
                    .IsUnicode(false)
                    .HasColumnName("trailerPhim");

                entity.Property(e => e.TrangThai)
                    .HasMaxLength(50)
                    .HasColumnName("trangThai");
            });

            modelBuilder.Entity<PhimTheLoai>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("Phim_theLoai");

                entity.Property(e => e.MaPhim).HasColumnName("maPhim");

                entity.Property(e => e.MaTheLoai).HasColumnName("maTheLoai");

                entity.HasOne(d => d.MaPhimNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.MaPhim)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Phim_theLoai_Phim");

                entity.HasOne(d => d.MaTheLoaiNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.MaTheLoai)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Phim_theLoai_theLoai");
            });

            modelBuilder.Entity<PhongChieu>(entity =>
            {
                entity.HasKey(e => e.MaPhong)
                    .HasName("PK__phongChi__4CD55E10A76D886B");

                entity.ToTable("phongChieu");

                entity.Property(e => e.MaPhong)
                    .ValueGeneratedNever()
                    .HasColumnName("maPhong");

                entity.Property(e => e.MaCumRap).HasColumnName("maCumRap");

                entity.Property(e => e.TenPhong)
                    .HasMaxLength(50)
                    .HasColumnName("tenPhong");

                entity.Property(e => e.TongGhe).HasColumnName("tongGhe");

                entity.HasOne(d => d.MaCumRapNavigation)
                    .WithMany(p => p.PhongChieus)
                    .HasForeignKey(d => d.MaCumRap)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_phongChieu_cumRap");
            });

            modelBuilder.Entity<PhimTheLoai>()
               .HasKey(pt => new { pt.MaPhim, pt.MaTheLoai });

            modelBuilder.Entity<PhimTheLoai>()
                .HasOne(pt => pt.MaPhimNavigation)
                .WithMany(p => p.PhimTheLoais)
                .HasForeignKey(pt => pt.MaPhim);

            modelBuilder.Entity<PhimTheLoai>()
                .HasOne(pt => pt.MaTheLoaiNavigation)
                .WithMany(t => t.PhimTheLoais)
                .HasForeignKey(pt => pt.MaTheLoai);

            modelBuilder.Entity<SuatChieu>(entity =>
            {
                entity.HasKey(e => e.MaSuat)
                    .HasName("PK__suatChie__D4930BB6C3A44A7F");

                entity.ToTable("suatChieu");

                entity.Property(e => e.MaSuat)
                    .ValueGeneratedNever()
                    .HasColumnName("maSuat");

                entity.Property(e => e.GioBatDau)
                    .HasColumnType("datetime")
                    .HasColumnName("gioBatDau");

                entity.Property(e => e.GioKetThuc)
                    .HasColumnType("datetime")
                    .HasColumnName("gioKetThuc");

                entity.Property(e => e.Gia)
                    .HasColumnType("money")
                    .HasColumnName("gia");

                entity.Property(e => e.MaPhim).HasColumnName("maPhim");

                entity.Property(e => e.MaPhong).HasColumnName("maPhong");

                entity.HasOne(d => d.MaPhimNavigation)
                    .WithMany(p => p.SuatChieus)
                    .HasForeignKey(d => d.MaPhim)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_suatChieu_Phim");

                entity.HasOne(d => d.MaPhongNavigation)
                    .WithMany(p => p.SuatChieus)
                    .HasForeignKey(d => d.MaPhong)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_suatChieu_phongChieu");
            });

            modelBuilder.Entity<TheLoai>(entity =>
            {
                entity.HasKey(e => e.MaTheLoai)
                    .HasName("PK__theLoai__2E9E267E8130A079");

                entity.ToTable("theLoai");

                entity.Property(e => e.MaTheLoai)
                    .ValueGeneratedNever()
                    .HasColumnName("maTheLoai");

                entity.Property(e => e.TenTheLoai)
                    .HasMaxLength(50)
                    .HasColumnName("tenTheLoai");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.MaUsers)
                    .HasName("PK__Users__B5C2199E670CC393");

                entity.HasIndex(e => e.SoDienThoai, "UQ__Users__06ACB9A25E115FD5")
                    .IsUnique();

                entity.Property(e => e.MaUsers)
                    .ValueGeneratedNever()
                    .HasColumnName("maUsers");

                entity.Property(e => e.Email)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("email");

                entity.Property(e => e.HoTen)
                    .HasMaxLength(100)
                    .HasColumnName("hoTen");

                entity.Property(e => e.MatKhau)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("matKhau");

                entity.Property(e => e.NgayTao)
                    .HasColumnType("datetime")
                    .HasColumnName("ngayTao");

                entity.Property(e => e.SoDienThoai)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasColumnName("soDienThoai");

                entity.Property(e => e.VaiTro)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasColumnName("vaiTro");
            });

            modelBuilder.Entity<UsersPhim>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("Users_Phim");

                entity.Property(e => e.BinhLuan)
                    .HasColumnType("text")
                    .HasColumnName("binhLuan");

                entity.Property(e => e.Diem).HasColumnName("diem");

                entity.Property(e => e.MaPhim).HasColumnName("maPhim");

                entity.Property(e => e.MaUsers).HasColumnName("maUsers");

                entity.HasOne(d => d.MaPhimNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.MaPhim)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Users_Phim_Phim");

                entity.HasOne(d => d.MaUsersNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.MaUsers)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Users_Phim_Users");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
