using System;
using System.Collections.Generic;
using HeThongThiDQ.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace HeThongThiDQ.Data;

public partial class ELEARNINGEntities : DbContext
{
    public ELEARNINGEntities(DbContextOptions<ELEARNINGEntities> options)
        : base(options)
    {
    }

    public virtual DbSet<BaiThi> BaiThis { get; set; }

    public virtual DbSet<CauHoi> CauHois { get; set; }

    public virtual DbSet<CauHoiDeThi> CauHoiDeThis { get; set; }

    public virtual DbSet<CtbaiThi> CtbaiThis { get; set; }

    public virtual DbSet<Ctlvdt> Ctlvdts { get; set; }

    public virtual DbSet<DanhSachDum> DanhSachDa { get; set; }

    public virtual DbSet<DeNghiKccd> DeNghiKccds { get; set; }

    public virtual DbSet<DeThi> DeThis { get; set; }

    public virtual DbSet<DinhBienVt> DinhBienVts { get; set; }

    public virtual DbSet<DsachDg> DsachDgs { get; set; }

    public virtual DbSet<Hdsd> Hdsds { get; set; }

    public virtual DbSet<HistoryLog> HistoryLogs { get; set; }

    public virtual DbSet<KccdBaiThi> KccdBaiThis { get; set; }

    public virtual DbSet<KccdCauHoi> KccdCauHois { get; set; }

    public virtual DbSet<KccdCtbaiThi> KccdCtbaiThis { get; set; }

    public virtual DbSet<KccdDeThi> KccdDeThis { get; set; }

    public virtual DbSet<KhungNangLuc> KhungNangLucs { get; set; }

    public virtual DbSet<Kip> Kips { get; set; }

    public virtual DbSet<KnlDg> KnlDgs { get; set; }

    public virtual DbSet<KnlDgiaTc> KnlDgiaTcs { get; set; }

    public virtual DbSet<KnlDocBangKnl> KnlDocBangKnls { get; set; }

    public virtual DbSet<KnlKhoi> KnlKhois { get; set; }

    public virtual DbSet<KnlKq> KnlKqs { get; set; }

    public virtual DbSet<KnlLoaiKq> KnlLoaiKqs { get; set; }

    public virtual DbSet<KnlLsdg> KnlLsdgs { get; set; }

    public virtual DbSet<KnlNhom> KnlNhoms { get; set; }

    public virtual DbSet<KnlNvkiemNhiem> KnlNvkiemNhiems { get; set; }

    public virtual DbSet<KnlPhanXuong> KnlPhanXuongs { get; set; }

    public virtual DbSet<KnlPheDuyetKnl> KnlPheDuyetKnls { get; set; }

    public virtual DbSet<KnlQuyen> KnlQuyens { get; set; }

    public virtual DbSet<KnlTo> KnlTos { get; set; }

    public virtual DbSet<LinhVucDt> LinhVucDts { get; set; }

    public virtual DbSet<ListController> ListControllers { get; set; }

    public virtual DbSet<LoaiKnl> LoaiKnls { get; set; }

    public virtual DbSet<LopHoc> LopHocs { get; set; }

    public virtual DbSet<LopHocDeThiPool> LopHocDeThiPools { get; set; }

    public virtual DbSet<NhanVien> NhanViens { get; set; }

    public virtual DbSet<NhomNlkccd> NhomNlkccds { get; set; }

    public virtual DbSet<NoiDungDt> NoiDungDts { get; set; }

    public virtual DbSet<NoiDungDtkccd> NoiDungDtkccds { get; set; }

    public virtual DbSet<NqChuong> NqChuongs { get; set; }

    public virtual DbSet<NqKetQua> NqKetQuas { get; set; }

    public virtual DbSet<PhanQuyenHt> PhanQuyenHts { get; set; }

    public virtual DbSet<PhieuXacNhanKccd> PhieuXacNhanKccds { get; set; }

    public virtual DbSet<PhongBan> PhongBans { get; set; }

    public virtual DbSet<QtBaiKiemTra> QtBaiKiemTras { get; set; }

    public virtual DbSet<QtCauHoiQt> QtCauHoiQts { get; set; }

    public virtual DbSet<QtCtbaiKiemTra> QtCtbaiKiemTras { get; set; }

    public virtual DbSet<QtDinhKy> QtDinhKies { get; set; }

    public virtual DbSet<QtFileQt> QtFileQts { get; set; }

    public virtual DbSet<QtLoaiQt> QtLoaiQts { get; set; }

    public virtual DbSet<QtNoiDungQt> QtNoiDungQts { get; set; }

    public virtual DbSet<QtPhanQuyen> QtPhanQuyens { get; set; }

    public virtual DbSet<QtVanBanLq> QtVanBanLqs { get; set; }

    public virtual DbSet<Quyen> Quyens { get; set; }

    public virtual DbSet<QuyenCn> QuyenCns { get; set; }

    public virtual DbSet<QuyenDetail> QuyenDetails { get; set; }

    public virtual DbSet<TbKyLuat> TbKyLuats { get; set; }

    public virtual DbSet<ThongBao> ThongBaos { get; set; }

    public virtual DbSet<TinhTrangLv> TinhTrangLvs { get; set; }

    public virtual DbSet<ViTriKnl> ViTriKnls { get; set; }

    public virtual DbSet<Vitri> Vitris { get; set; }

    public virtual DbSet<XnhocTap> XnhocTaps { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BaiThi>(entity =>
        {
            entity.HasKey(e => e.IdbaiThi);

            entity.ToTable("BaiThi");

            entity.Property(e => e.IdbaiThi).HasColumnName("IDBaiThi");
            entity.Property(e => e.GioBatDau).HasColumnType("datetime");
            entity.Property(e => e.GioKetThuc).HasColumnType("datetime");
            entity.Property(e => e.IddeThi).HasColumnName("IDDeThi");
            entity.Property(e => e.Idlh).HasColumnName("IDLH");
            entity.Property(e => e.Idnd).HasColumnName("IDND");
            entity.Property(e => e.Idnv).HasColumnName("IDNV");
            entity.Property(e => e.IdphongBan).HasColumnName("IDPhongBan");
            entity.Property(e => e.IdviTri).HasColumnName("IDViTri");
            entity.Property(e => e.LanThi).HasDefaultValue(1);
            entity.Property(e => e.TinhTrang).HasDefaultValue(false);
        });

        modelBuilder.Entity<CauHoi>(entity =>
        {
            entity.HasKey(e => e.Idch);

            entity.ToTable("CauHoi");

            entity.Property(e => e.Idch).HasColumnName("IDCH");
            entity.Property(e => e.DapAnA).HasMaxLength(450);
            entity.Property(e => e.DapAnB).HasMaxLength(450);
            entity.Property(e => e.DapAnC).HasMaxLength(450);
            entity.Property(e => e.DapAnD).HasMaxLength(450);
            entity.Property(e => e.Gvid).HasColumnName("GVID");
            entity.Property(e => e.Iddađung).HasColumnName("IDDAĐung");
            entity.Property(e => e.Idnd).HasColumnName("IDND");
            entity.Property(e => e.IsDao).HasDefaultValue(true);
            entity.Property(e => e.MaCh)
                .HasMaxLength(10)
                .HasColumnName("MaCH");
            entity.Property(e => e.NoiDungCh)
                .HasMaxLength(500)
                .HasColumnName("NoiDungCH");
        });

        modelBuilder.Entity<CauHoiDeThi>(entity =>
        {
            entity.HasKey(e => e.IdcauHoiDeThi);

            entity.ToTable("CauHoiDeThi");

            entity.Property(e => e.IdcauHoiDeThi).HasColumnName("IDCauHoiDeThi");
            entity.Property(e => e.IdcauHoi).HasColumnName("IDCauHoi");
            entity.Property(e => e.IddeThi).HasColumnName("IDDeThi");

            entity.HasOne(d => d.IdcauHoiNavigation).WithMany(p => p.CauHoiDeThis)
                .HasForeignKey(d => d.IdcauHoi)
                .HasConstraintName("FK_CauHoiDeThi_CauHoi");

            entity.HasOne(d => d.IddeThiNavigation).WithMany(p => p.CauHoiDeThis)
                .HasForeignKey(d => d.IddeThi)
                .HasConstraintName("FK_CauHoiDeThi_DeThi");
        });

        modelBuilder.Entity<CtbaiThi>(entity =>
        {
            entity.HasKey(e => e.Idctbt);

            entity.ToTable("CTBaiThi");

            entity.Property(e => e.Idctbt).HasColumnName("IDCTBT");
            entity.Property(e => e.IdbaiThi).HasColumnName("IDBaiThi");
            entity.Property(e => e.IdcauHoi).HasColumnName("IDCauHoi");
            entity.Property(e => e.IddapAnDung).HasColumnName("IDDapAnDung");
            entity.Property(e => e.IddapAnNv).HasColumnName("IDDApAnNV");
        });

        modelBuilder.Entity<Ctlvdt>(entity =>
        {
            entity.HasKey(e => e.Idctlvdt);

            entity.ToTable("CTLVDT");

            entity.Property(e => e.Idctlvdt).HasColumnName("IDCTLVDT");
            entity.Property(e => e.Lvdtid).HasColumnName("LVDTID");
            entity.Property(e => e.TenCtlvdt)
                .HasMaxLength(150)
                .HasColumnName("TenCTLVDT");

            entity.HasOne(d => d.Lvdt).WithMany(p => p.Ctlvdts)
                .HasForeignKey(d => d.Lvdtid)
                .HasConstraintName("FK_CTLVDT_LinhVucDT");
        });

        modelBuilder.Entity<DanhSachDum>(entity =>
        {
            entity.HasKey(e => e.Iddsđa);

            entity.ToTable("DanhSachDA");

            entity.Property(e => e.Iddsđa)
                .ValueGeneratedNever()
                .HasColumnName("IDDSĐA");
            entity.Property(e => e.TenĐa)
                .HasMaxLength(50)
                .HasColumnName("TenĐA");
        });

        modelBuilder.Entity<DeNghiKccd>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_PhieuKCCD");

            entity.ToTable("DeNghiKCCD");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.DeThiId).HasColumnName("DeThiID");
            entity.Property(e => e.IsKiemTra).HasColumnName("isKiemTra");
            entity.Property(e => e.LinhVucId).HasColumnName("LinhVucID");
            entity.Property(e => e.NgayTao).HasColumnType("smalldatetime");
            entity.Property(e => e.NgayXn)
                .HasColumnType("smalldatetime")
                .HasColumnName("NgayXN");
            entity.Property(e => e.NhomNangLucId).HasColumnName("NhomNangLucID");
            entity.Property(e => e.NoiDungKccdid).HasColumnName("NoiDungKCCDID");
            entity.Property(e => e.PhongBanId).HasColumnName("PhongBanID");
            entity.Property(e => e.ViTriId1).HasColumnName("ViTriID1");
            entity.Property(e => e.ViTriId2).HasColumnName("ViTriID2");

            entity.HasOne(d => d.NoiDungKccd).WithMany(p => p.DeNghiKccds)
                .HasForeignKey(d => d.NoiDungKccdid)
                .HasConstraintName("FK_DeNghiKCCD_NoiDungDTKCCD");
        });

        modelBuilder.Entity<DeThi>(entity =>
        {
            entity.HasKey(e => e.IddeThi);

            entity.ToTable("DeThi");

            entity.Property(e => e.IddeThi).HasColumnName("IDDeThi");
            entity.Property(e => e.CtdtId).HasColumnName("CTDT_ID");
            entity.Property(e => e.FileDeThi).HasMaxLength(250);
            entity.Property(e => e.Gvid).HasColumnName("GVID");
            entity.Property(e => e.Idnd).HasColumnName("IDND");
            entity.Property(e => e.MaDe).HasMaxLength(50);
            entity.Property(e => e.TenDe).HasMaxLength(100);

            entity.HasOne(d => d.IdndNavigation).WithMany(p => p.DeThis)
                .HasForeignKey(d => d.Idnd)
                .HasConstraintName("FK_DeThi_NoiDungDT");
        });

        modelBuilder.Entity<DinhBienVt>(entity =>
        {
            entity.ToTable("DinhBienVT");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.GhiChu).HasMaxLength(50);
            entity.Property(e => e.Idvt).HasColumnName("IDVT");
            entity.Property(e => e.NgayTao).HasColumnType("datetime");
            entity.Property(e => e.NstapSu).HasColumnName("NSTapSu");
            entity.Property(e => e.Nstrong).HasColumnName("NSTrong");
            entity.Property(e => e.Nsvtkhac).HasColumnName("NSVTKhac");
            entity.Property(e => e.SldinhBien).HasColumnName("SLDinhBien");
            entity.Property(e => e.TgboNhiem).HasColumnName("TGBoNhiem");
        });

        modelBuilder.Entity<DsachDg>(entity =>
        {
            entity.HasKey(e => e.Idds);

            entity.ToTable("DSachDG");

            entity.Property(e => e.Idds).HasColumnName("IDDS");
            entity.Property(e => e.Idknl).HasColumnName("IDKNL");
            entity.Property(e => e.MaNv)
                .HasMaxLength(10)
                .HasColumnName("MaNV");
            entity.Property(e => e.Nvdg)
                .HasMaxLength(10)
                .HasColumnName("NVDG");
        });

        modelBuilder.Entity<Hdsd>(entity =>
        {
            entity.ToTable("HDSD");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.MoTa).HasMaxLength(200);
        });

        modelBuilder.Entity<HistoryLog>(entity =>
        {
            entity.ToTable("HistoryLog");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.DeviceId).HasMaxLength(100);
            entity.Property(e => e.ExpireTime).HasColumnType("datetime");
            entity.Property(e => e.NhanVienId).HasColumnName("NhanVienID");
        });

        modelBuilder.Entity<KccdBaiThi>(entity =>
        {
            entity.ToTable("KCCD_BaiThi");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.DeNghiId).HasColumnName("DeNghiID");
            entity.Property(e => e.DeThiId).HasColumnName("DeThiID");
            entity.Property(e => e.Idnv).HasColumnName("IDNV");
            entity.Property(e => e.Kccdid).HasColumnName("KCCDID");
        });

        modelBuilder.Entity<KccdCauHoi>(entity =>
        {
            entity.HasKey(e => e.Idch);

            entity.ToTable("KCCD_CauHoi");

            entity.Property(e => e.Idch).HasColumnName("IDCH");
            entity.Property(e => e.DapAnA).HasMaxLength(250);
            entity.Property(e => e.DapAnB).HasMaxLength(250);
            entity.Property(e => e.DapAnC).HasMaxLength(250);
            entity.Property(e => e.DapAnD).HasMaxLength(250);
            entity.Property(e => e.DeThiId).HasColumnName("DeThiID");
            entity.Property(e => e.Iddađung).HasColumnName("IDDAĐung");
            entity.Property(e => e.Kccdid).HasColumnName("KCCDID");
            entity.Property(e => e.NoiDungCh)
                .HasMaxLength(500)
                .HasColumnName("NoiDungCH");

            entity.HasOne(d => d.DeThi).WithMany(p => p.KccdCauHois)
                .HasForeignKey(d => d.DeThiId)
                .HasConstraintName("FK_KCCD_CauHoi_KCCD_DeThi");
        });

        modelBuilder.Entity<KccdCtbaiThi>(entity =>
        {
            entity.HasKey(e => e.Idct);

            entity.ToTable("KCCD_CTBaiThi");

            entity.Property(e => e.Idct).HasColumnName("IDCT");
            entity.Property(e => e.DapAnHv).HasColumnName("DapAnHV");
            entity.Property(e => e.Idbt).HasColumnName("IDBT");
            entity.Property(e => e.IdcauHoi).HasColumnName("IDCauHoi");
            entity.Property(e => e.Iddađung).HasColumnName("IDDAĐung");

            entity.HasOne(d => d.IdbtNavigation).WithMany(p => p.KccdCtbaiThis)
                .HasForeignKey(d => d.Idbt)
                .HasConstraintName("FK_KCCD_CTBaiThi_KCCD_BaiThi");

            entity.HasOne(d => d.IdcauHoiNavigation).WithMany(p => p.KccdCtbaiThis)
                .HasForeignKey(d => d.IdcauHoi)
                .HasConstraintName("FK_KCCD_CTBaiThi_KCCD_CauHoi");
        });

        modelBuilder.Entity<KccdDeThi>(entity =>
        {
            entity.ToTable("KCCD_DeThi");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Kccdid).HasColumnName("KCCDID");
            entity.Property(e => e.MaDe).HasMaxLength(50);
            entity.Property(e => e.TenDe).HasMaxLength(250);
        });

        modelBuilder.Entity<KhungNangLuc>(entity =>
        {
            entity.HasKey(e => e.Idnl);

            entity.ToTable("KhungNangLuc");

            entity.Property(e => e.Idnl).HasColumnName("IDNL");
            entity.Property(e => e.IdloaiNl).HasColumnName("IDLoaiNL");
            entity.Property(e => e.Idpb).HasColumnName("IDPB");
            entity.Property(e => e.Idvt).HasColumnName("IDVT");
            entity.Property(e => e.IsDanhGia).HasDefaultValue(1);
            entity.Property(e => e.OrderBy).HasDefaultValue(1);
            entity.Property(e => e.TenNl)
                .HasMaxLength(1000)
                .HasColumnName("TenNL");
        });

        modelBuilder.Entity<Kip>(entity =>
        {
            entity.HasKey(e => e.Idkip);

            entity.ToTable("Kip");

            entity.Property(e => e.Idkip).HasColumnName("IDKip");
            entity.Property(e => e.TenKip).HasMaxLength(50);
        });

        modelBuilder.Entity<KnlDg>(entity =>
        {
            entity.ToTable("KNL_DG");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Idvtddg).HasColumnName("IDVTDDG");
            entity.Property(e => e.Idvtdg).HasColumnName("IDVTDG");
        });

        modelBuilder.Entity<KnlDgiaTc>(entity =>
        {
            entity.ToTable("KNL_DGiaTC");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Idvt).HasColumnName("IDVT");
            entity.Property(e => e.Idvtdgtc).HasColumnName("IDVTDGTC");
            entity.Property(e => e.Idvtdgtt).HasColumnName("IDVTDGTT");
        });

        modelBuilder.Entity<KnlDocBangKnl>(entity =>
        {
            entity.ToTable("KNL_DocBangKNL");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.IdNangLuc).HasColumnName("ID_NangLuc");
            entity.Property(e => e.IdViTriKnl).HasColumnName("ID_ViTriKNL");
            entity.Property(e => e.Idnv).HasColumnName("IDNV");
            entity.Property(e => e.IsDelete).HasDefaultValue(false);
            entity.Property(e => e.NgayTao).HasColumnType("datetime");
            entity.Property(e => e.TinhTrang).HasDefaultValue(1);
        });

        modelBuilder.Entity<KnlKhoi>(entity =>
        {
            entity.ToTable("KNL_Khoi");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.MaKhoi).HasMaxLength(30);
            entity.Property(e => e.TenKhoi).HasMaxLength(50);
        });

        modelBuilder.Entity<KnlKq>(entity =>
        {
            entity.HasKey(e => e.Idkq);

            entity.ToTable("KNL_KQ");

            entity.Property(e => e.Idkq).HasColumnName("IDKQ");
            entity.Property(e => e.DiemDg).HasColumnName("DiemDG");
            entity.Property(e => e.DiemDgLan1).HasColumnName("DiemDG_Lan1");
            entity.Property(e => e.DiemDm).HasColumnName("DiemDM");
            entity.Property(e => e.DiemTuDg).HasColumnName("DiemTuDG");
            entity.Property(e => e.IdnguoiDgLan1).HasColumnName("IDNguoiDG_Lan1");
            entity.Property(e => e.Idnl).HasColumnName("IDNL");
            entity.Property(e => e.Idnv).HasColumnName("IDNV");
            entity.Property(e => e.Idnvdg).HasColumnName("IDNVDG");
            entity.Property(e => e.Kqid).HasColumnName("KQID");
            entity.Property(e => e.NgayDg).HasColumnName("NgayDG");
            entity.Property(e => e.NgayDgLan1).HasColumnName("NgayDG_Lan1");
            entity.Property(e => e.NgayTuDg).HasColumnName("NgayTuDG");
            entity.Property(e => e.Note).HasMaxLength(50);
            entity.Property(e => e.ThangDg).HasColumnName("ThangDG");
            entity.Property(e => e.Vtid).HasColumnName("VTID");
        });

        modelBuilder.Entity<KnlLoaiKq>(entity =>
        {
            entity.HasKey(e => e.Idkq);

            entity.ToTable("KNL_LoaiKQ");

            entity.Property(e => e.Idkq).HasColumnName("IDKQ");
            entity.Property(e => e.TenKq)
                .HasMaxLength(20)
                .HasColumnName("TenKQ");
        });

        modelBuilder.Entity<KnlLsdg>(entity =>
        {
            entity.HasKey(e => e.Idls);

            entity.ToTable("KNL_LSDG");

            entity.Property(e => e.Idls).HasColumnName("IDLS");
            entity.Property(e => e.Chuadg).HasColumnName("CHUADG");
            entity.Property(e => e.ChuadgtuDg).HasColumnName("CHUADGTuDG");
            entity.Property(e => e.ChuadgtuDglan1).HasColumnName("CHUADGTuDGLan1");
            entity.Property(e => e.Dat).HasColumnName("DAT");
            entity.Property(e => e.Dattudg).HasColumnName("DATTUDG");
            entity.Property(e => e.Dattudglan1).HasColumnName("DATTUDGLan1");
            entity.Property(e => e.Kdat).HasColumnName("KDAT");
            entity.Property(e => e.Kdattudg).HasColumnName("KDATTUDG");
            entity.Property(e => e.Kdattudglan1).HasColumnName("KDATTUDGLan1");
            entity.Property(e => e.Kdgia).HasColumnName("KDGia");
            entity.Property(e => e.KdgiaTuDg).HasColumnName("KDGiaTuDG");
            entity.Property(e => e.KdgiaTuDglan1).HasColumnName("KDGiaTuDGLan1");
            entity.Property(e => e.NgayDggn).HasColumnName("NgayDGGN");
            entity.Property(e => e.NgayDggnlan1).HasColumnName("NgayDGGNLan1");
            entity.Property(e => e.NgayTuDggn).HasColumnName("NgayTuDGGN");
            entity.Property(e => e.Nvid).HasColumnName("NVID");
            entity.Property(e => e.ThangDg).HasColumnName("ThangDG");
            entity.Property(e => e.Tongnl).HasColumnName("TONGNL");
            entity.Property(e => e.Vtid).HasColumnName("VTID");
            entity.Property(e => e.Vuot).HasColumnName("VUOT");
            entity.Property(e => e.Vuottudg).HasColumnName("VUOTTUDG");
            entity.Property(e => e.Vuottudglan1).HasColumnName("VUOTTUDGLan1");
        });

        modelBuilder.Entity<KnlNhom>(entity =>
        {
            entity.HasKey(e => e.Idnhom);

            entity.ToTable("KNL_Nhom");

            entity.Property(e => e.Idnhom).HasColumnName("IDNhom");
            entity.Property(e => e.Idkhoi).HasColumnName("IDKhoi");
            entity.Property(e => e.IdphanXuong).HasColumnName("IDPhanXuong");
            entity.Property(e => e.IdphongBan).HasColumnName("IDPhongBan");
            entity.Property(e => e.MaNhom).HasMaxLength(50);
            entity.Property(e => e.TenNhom).HasMaxLength(200);
        });

        modelBuilder.Entity<KnlNvkiemNhiem>(entity =>
        {
            entity.ToTable("KNL_NVKiemNhiem");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Idnv).HasColumnName("IDNV");
            entity.Property(e => e.Idvtkn).HasColumnName("IDVTKN");
        });

        modelBuilder.Entity<KnlPhanXuong>(entity =>
        {
            entity.ToTable("KNL_PhanXuong");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Idkhoi).HasColumnName("IDKhoi");
            entity.Property(e => e.IdphongBan).HasColumnName("IDPhongBan");
            entity.Property(e => e.MaPx)
                .HasMaxLength(50)
                .HasColumnName("MaPX");
            entity.Property(e => e.TenPx)
                .HasMaxLength(100)
                .HasColumnName("TenPX");
        });

        modelBuilder.Entity<KnlPheDuyetKnl>(entity =>
        {
            entity.ToTable("KNL_PheDuyetKNL");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FileKnl)
                .HasMaxLength(500)
                .HasColumnName("File_KNL");
            entity.Property(e => e.IdNguoiDuyet).HasColumnName("ID_NguoiDuyet");
            entity.Property(e => e.IdNguoiTao).HasColumnName("ID_NguoiTao");
            entity.Property(e => e.Idvt).HasColumnName("IDVT");
            entity.Property(e => e.NgayDuyet).HasColumnType("datetime");
            entity.Property(e => e.NgayTrinhKy).HasColumnType("datetime");
            entity.Property(e => e.TinhTrang).HasDefaultValue(0);

            entity.HasOne(d => d.IdNguoiDuyetNavigation).WithMany(p => p.KnlPheDuyetKnlIdNguoiDuyetNavigations)
                .HasForeignKey(d => d.IdNguoiDuyet)
                .HasConstraintName("FK_KNL_PheDuyetKNL_NhanVien");

            entity.HasOne(d => d.IdNguoiTaoNavigation).WithMany(p => p.KnlPheDuyetKnlIdNguoiTaoNavigations)
                .HasForeignKey(d => d.IdNguoiTao)
                .HasConstraintName("FK_KNL_PheDuyetKNL_NhanVien1");
        });

        modelBuilder.Entity<KnlQuyen>(entity =>
        {
            entity.HasKey(e => e.Idquyen);

            entity.ToTable("KNL_Quyen");

            entity.Property(e => e.Idquyen).HasColumnName("IDQuyen");
            entity.Property(e => e.TenQuyen).HasMaxLength(50);
        });

        modelBuilder.Entity<KnlTo>(entity =>
        {
            entity.HasKey(e => e.Idto);

            entity.ToTable("KNL_To");

            entity.Property(e => e.Idto).HasColumnName("IDTo");
            entity.Property(e => e.Idkhoi).HasColumnName("IDKhoi");
            entity.Property(e => e.IdphanXuong).HasColumnName("IDPhanXuong");
            entity.Property(e => e.IdphongBan).HasColumnName("IDPhongBan");
            entity.Property(e => e.MaTo).HasMaxLength(20);
            entity.Property(e => e.TenTo).HasMaxLength(200);
        });

        modelBuilder.Entity<LinhVucDt>(entity =>
        {
            entity.HasKey(e => e.Idlvdt);

            entity.ToTable("LinhVucDT");

            entity.Property(e => e.Idlvdt).HasColumnName("IDLVDT");
            entity.Property(e => e.TenLvdt)
                .HasMaxLength(150)
                .HasColumnName("TenLVDT");
        });

        modelBuilder.Entity<ListController>(entity =>
        {
            entity.ToTable("ListController");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Controller).HasMaxLength(50);
            entity.Property(e => e.DsquyenCn)
                .HasMaxLength(50)
                .HasColumnName("DSQuyenCN");
            entity.Property(e => e.IsActive).HasColumnName("isActive");
            entity.Property(e => e.Mota).HasMaxLength(100);
        });

        modelBuilder.Entity<LoaiKnl>(entity =>
        {
            entity.HasKey(e => e.Idloai);

            entity.ToTable("LoaiKNL");

            entity.Property(e => e.Idloai).HasColumnName("IDLoai");
            entity.Property(e => e.Idvt).HasColumnName("IDVT");
            entity.Property(e => e.OrderBy).HasDefaultValue(1);
        });

        modelBuilder.Entity<LopHoc>(entity =>
        {
            entity.HasKey(e => e.Idlh);

            entity.ToTable("LopHoc");

            entity.Property(e => e.Idlh).HasColumnName("IDLH");
            entity.Property(e => e.BoPhanId).HasColumnName("BoPhan_ID");
            entity.Property(e => e.CtdtId).HasColumnName("CTDT_ID");
            entity.Property(e => e.DiaDiemDt)
                .HasMaxLength(100)
                .HasColumnName("DiaDiemDT");
            entity.Property(e => e.DonViGv).HasColumnName("DonVi_GV");
            entity.Property(e => e.Gvid).HasColumnName("GVID");
            entity.Property(e => e.IddeThi).HasColumnName("IDDeThi");
            entity.Property(e => e.IsCoCtdt).HasColumnName("IsCoCTDT");
            entity.Property(e => e.MaLh)
                .HasMaxLength(20)
                .HasColumnName("MaLH");
            entity.Property(e => e.NamDt).HasColumnName("NamDT");
            entity.Property(e => e.NcdtId).HasColumnName("NCDT_ID");
            entity.Property(e => e.Ndid).HasColumnName("NDID");
            entity.Property(e => e.NgayKiemTra).HasColumnType("datetime");
            entity.Property(e => e.NgayTao).HasColumnType("datetime");
            entity.Property(e => e.NguoiKiemTraId).HasColumnName("NguoiKiemTra_ID");
            entity.Property(e => e.NguoiTaoId).HasColumnName("NguoiTao_ID");
            entity.Property(e => e.NoiDungTrichYeu).HasMaxLength(200);
            entity.Property(e => e.QuyDt).HasColumnName("QuyDT");
            entity.Property(e => e.TenLh)
                .HasMaxLength(150)
                .HasColumnName("TenLH");
            entity.Property(e => e.Tgbdlh)
                .HasColumnType("datetime")
                .HasColumnName("TGBDLH");
            entity.Property(e => e.Tgktlh)
                .HasColumnType("datetime")
                .HasColumnName("TGKTLH");
            entity.Property(e => e.ThoiLuongDt)
                .HasMaxLength(100)
                .HasColumnName("ThoiLuongDT");
            entity.Property(e => e.ToChucThi).HasDefaultValue(true);

            entity.HasOne(d => d.Nd).WithMany(p => p.LopHocs)
                .HasForeignKey(d => d.Ndid)
                .HasConstraintName("FK_LopHoc_NoiDungDT");
        });

        modelBuilder.Entity<LopHocDeThiPool>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("LopHocDeThiPool");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Idlh).HasColumnName("IDLH");
            entity.Property(e => e.IddeThi).HasColumnName("IDDeThi");
            entity.HasIndex(e => new { e.Idlh, e.IddeThi }).IsUnique();
        });

        modelBuilder.Entity<NhanVien>(entity =>
        {
            entity.ToTable("NhanVien");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.ChuDeThi).HasMaxLength(50);
            entity.Property(e => e.ChuKy).HasMaxLength(100);
            entity.Property(e => e.CongTyCapC2).HasMaxLength(100);
            entity.Property(e => e.DiaChi).HasMaxLength(150);
            entity.Property(e => e.DienThoai)
                .HasMaxLength(11)
                .IsUnicode(false);
            entity.Property(e => e.DonViToChucC4).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(50);
            entity.Property(e => e.HoTen).HasMaxLength(50);
            entity.Property(e => e.HoTenKhongDau).HasMaxLength(50);
            entity.Property(e => e.Idkip).HasColumnName("IDKip");
            entity.Property(e => e.IdphongBan).HasColumnName("IDPhongBan");
            entity.Property(e => e.Idquyen)
                .HasDefaultValue(4)
                .HasColumnName("IDQuyen");
            entity.Property(e => e.IdquyenKnl).HasColumnName("IDQuyenKNL");
            entity.Property(e => e.IdtinhTrangLv).HasColumnName("IDTinhTrangLV");
            entity.Property(e => e.IdviTri).HasColumnName("IDViTri");
            entity.Property(e => e.Idvtknl).HasColumnName("IDVTKNL");
            entity.Property(e => e.IsGv)
                .HasDefaultValue(false)
                .HasColumnName("IsGV");
            entity.Property(e => e.MaNv)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("MaNV");
            entity.Property(e => e.MaViTri)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MatKhau)
                .HasMaxLength(50)
                .HasDefaultValue("ceea23519f6f86ad67e9f798bf8002cb");
            entity.Property(e => e.TongCongTy).HasMaxLength(250);

            entity.HasOne(d => d.IdphongBanNavigation).WithMany(p => p.NhanViens)
                .HasForeignKey(d => d.IdphongBan)
                .HasConstraintName("FK_NhanVien_PhongBan");

            entity.HasOne(d => d.IdtinhTrangLvNavigation).WithMany(p => p.NhanViens)
                .HasForeignKey(d => d.IdtinhTrangLv)
                .HasConstraintName("FK_NhanVien_TinhTrangLV");

            entity.HasOne(d => d.IdviTriNavigation).WithMany(p => p.NhanViens)
                .HasForeignKey(d => d.IdviTri)
                .HasConstraintName("FK_NhanVien_Vitri");
        });

        modelBuilder.Entity<NhomNlkccd>(entity =>
        {
            entity.ToTable("NhomNLKCCD");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.NoiDung).HasMaxLength(150);
        });

        modelBuilder.Entity<NoiDungDt>(entity =>
        {
            entity.HasKey(e => e.Idnd).HasName("PK_KhoaHoc");

            entity.ToTable("NoiDungDT");

            entity.Property(e => e.Idnd).HasColumnName("IDND");
            entity.Property(e => e.Bplid).HasColumnName("BPLID");
            entity.Property(e => e.FileDinhKem).HasMaxLength(300);
            entity.Property(e => e.Idctlvdt).HasColumnName("IDCTLVDT");
            entity.Property(e => e.IdhoatDongDt).HasColumnName("IDHoatDongDT");
            entity.Property(e => e.IdnguonGv).HasColumnName("IDNguonGV");
            entity.Property(e => e.IdnhomNl).HasColumnName("IDNhomNL");
            entity.Property(e => e.IdphanLoaiDt).HasColumnName("IDPhanLoaiDT");
            entity.Property(e => e.IdphuongPhapDt).HasColumnName("IDPhuongPhapDT");
            entity.Property(e => e.ImageNd)
                .HasMaxLength(300)
                .HasColumnName("ImageND");
            entity.Property(e => e.IsDelete).HasDefaultValue(false);
            entity.Property(e => e.IsNq).HasColumnName("isNQ");
            entity.Property(e => e.IsOrder).HasColumnName("isOrder");
            entity.Property(e => e.LoaiNcdtId).HasColumnName("LoaiNCDT_ID");
            entity.Property(e => e.Lvdtid).HasColumnName("LVDTID");
            entity.Property(e => e.MaNd)
                .HasMaxLength(20)
                .HasColumnName("MaND");
            entity.Property(e => e.NoiDung).HasMaxLength(600);
            entity.Property(e => e.ThoiLuongDt).HasColumnName("ThoiLuongDT");
            entity.Property(e => e.VideoNd)
                .HasMaxLength(300)
                .HasColumnName("VideoND");

            entity.HasOne(d => d.IdnhomNlNavigation).WithMany(p => p.NoiDungDts)
                .HasForeignKey(d => d.IdnhomNl)
                .HasConstraintName("FK_NoiDungDT_NhomNLKCCD");

            entity.HasOne(d => d.Lvdt).WithMany(p => p.NoiDungDts)
                .HasForeignKey(d => d.Lvdtid)
                .HasConstraintName("FK_NoiDungDT_LinhVucDT");
        });

        modelBuilder.Entity<NoiDungDtkccd>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_NoiDungDKCCD");

            entity.ToTable("NoiDungDTKCCD");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Lvdtid).HasColumnName("LVDTID");
            entity.Property(e => e.NgayTao).HasColumnType("smalldatetime");
            entity.Property(e => e.NhomNlid).HasColumnName("NhomNLID");
            entity.Property(e => e.PhongBanId).HasColumnName("PhongBanID");
            entity.Property(e => e.TenNd)
                .HasMaxLength(150)
                .HasColumnName("TenND");

            entity.HasOne(d => d.NhomNl).WithMany(p => p.NoiDungDtkccds)
                .HasForeignKey(d => d.NhomNlid)
                .HasConstraintName("FK_NoiDungDTKCCD_NhomNLKCCD");
        });

        modelBuilder.Entity<NqChuong>(entity =>
        {
            entity.ToTable("NQ_Chuong");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.IsOrder).HasColumnName("isOrder");
            entity.Property(e => e.MaChuong).HasMaxLength(20);
            entity.Property(e => e.Slht)
                .HasDefaultValue(0)
                .HasColumnName("SLHT");
            entity.Property(e => e.Slhtfile).HasColumnName("SLHTFile");
            entity.Property(e => e.Sltg)
                .HasDefaultValue(0)
                .HasColumnName("SLTG");
            entity.Property(e => e.TenChuong).HasMaxLength(250);
        });

        modelBuilder.Entity<NqKetQua>(entity =>
        {
            entity.ToTable("NQ_KetQua");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Idnv).HasColumnName("IDNV");
            entity.Property(e => e.Nddtid).HasColumnName("NDDTID");
            entity.Property(e => e.NgayHt).HasColumnName("NgayHT");
            entity.Property(e => e.NgayTg).HasColumnName("NgayTG");
            entity.Property(e => e.Xnht).HasColumnName("XNHT");
            entity.Property(e => e.Xnhtfile).HasColumnName("XNHTFile");
            entity.Property(e => e.Xntg).HasColumnName("XNTG");
        });

        modelBuilder.Entity<PhanQuyenHt>(entity =>
        {
            entity.ToTable("PhanQuyenHT");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Idnv).HasColumnName("IDNV");
            entity.Property(e => e.Idquyen).HasColumnName("IDQuyen");
        });

        modelBuilder.Entity<PhieuXacNhanKccd>(entity =>
        {
            entity.ToTable("PhieuXacNhanKCCD");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.DeNghiDtid).HasColumnName("DeNghiDTID");
            entity.Property(e => e.GvketLuan).HasColumnName("GVKetLuan");
            entity.Property(e => e.GvketLuanYkienKhac)
                .HasMaxLength(150)
                .HasColumnName("GVKetLuanYKienKhac");
            entity.Property(e => e.GvlyThuyetSauDt).HasColumnName("GVLyThuyetSauDT");
            entity.Property(e => e.GvlyThuyetTruocDt).HasColumnName("GVLyThuyetTruocDT");
            entity.Property(e => e.GvnhanXetLtsauDt)
                .HasMaxLength(150)
                .HasColumnName("GVNhanXetLTSauDT");
            entity.Property(e => e.GvnhanXetLttruocDt)
                .HasMaxLength(150)
                .HasColumnName("GVNhanXetLTTruocDT");
            entity.Property(e => e.GvnhanXetThsauDt)
                .HasMaxLength(150)
                .HasColumnName("GVNhanXetTHSauDT");
            entity.Property(e => e.GvnhanXetThtruocDt)
                .HasMaxLength(150)
                .HasColumnName("GVNhanXetTHTruocDT");
            entity.Property(e => e.GvthucHanhSauDt).HasColumnName("GVThucHanhSauDT");
            entity.Property(e => e.GvthucHanhTruocDt).HasColumnName("GVThucHanhTruocDT");
            entity.Property(e => e.HocVienId).HasColumnName("HocVienID");
            entity.Property(e => e.HvdeXuat).HasColumnName("HVDeXuat");
            entity.Property(e => e.HvdeXuatKhac)
                .HasMaxLength(150)
                .HasColumnName("HVDeXuatKhac");
            entity.Property(e => e.HvngayXacNhan)
                .HasColumnType("smalldatetime")
                .HasColumnName("HVNgayXacNhan");
            entity.Property(e => e.HvsauCanCaiThien)
                .HasMaxLength(150)
                .HasColumnName("HVSauCanCaiThien");
            entity.Property(e => e.HvsauDatDuoc)
                .HasMaxLength(150)
                .HasColumnName("HVSauDatDuoc");
            entity.Property(e => e.HvtruocCanCaiThien)
                .HasMaxLength(150)
                .HasColumnName("HVTruocCanCaiThien");
            entity.Property(e => e.HvtruocDatDuoc)
                .HasMaxLength(150)
                .HasColumnName("HVTruocDatDuoc");
            entity.Property(e => e.IdtinhTrang).HasColumnName("IDTinhTrang");

            entity.HasOne(d => d.DeNghiDt).WithMany(p => p.PhieuXacNhanKccds)
                .HasForeignKey(d => d.DeNghiDtid)
                .HasConstraintName("FK_PhieuXacNhanKCCD_DeNghiKCCD");
        });

        modelBuilder.Entity<PhongBan>(entity =>
        {
            entity.HasKey(e => e.IdphongBan);

            entity.ToTable("PhongBan");

            entity.Property(e => e.IdphongBan).HasColumnName("IDPhongBan");
            entity.Property(e => e.ApiMaPb)
                .HasMaxLength(10)
                .HasColumnName("API_MaPB");
            entity.Property(e => e.MaPb)
                .HasMaxLength(20)
                .HasColumnName("MaPB");
            entity.Property(e => e.TenPhongBan).HasMaxLength(50);
        });

        modelBuilder.Entity<QtBaiKiemTra>(entity =>
        {
            entity.HasKey(e => e.Idkt);

            entity.ToTable("QT_BaiKiemTra");

            entity.Property(e => e.Idkt).HasColumnName("IDKT");
            entity.Property(e => e.Idnv).HasColumnName("IDNV");
            entity.Property(e => e.LanKt).HasColumnName("LanKT");
            entity.Property(e => e.NgayHt).HasColumnName("NgayHT");
            entity.Property(e => e.NgayKt).HasColumnName("NgayKT");
            entity.Property(e => e.NgayKttt).HasColumnName("NgayKTTT");
            entity.Property(e => e.Qthdid).HasColumnName("QTHDID");

            entity.HasOne(d => d.Qthd).WithMany(p => p.QtBaiKiemTras)
                .HasForeignKey(d => d.Qthdid)
                .HasConstraintName("FK_QT_BaiKiemTra_QT_NoiDungQT");
        });

        modelBuilder.Entity<QtCauHoiQt>(entity =>
        {
            entity.HasKey(e => e.Idch);

            entity.ToTable("QT_CauHoiQT");

            entity.Property(e => e.Idch).HasColumnName("IDCH");
            entity.Property(e => e.DapAnA).HasMaxLength(250);
            entity.Property(e => e.DapAnB).HasMaxLength(250);
            entity.Property(e => e.DapAnC).HasMaxLength(250);
            entity.Property(e => e.DapAnD).HasMaxLength(250);
            entity.Property(e => e.Iddađung).HasColumnName("IDDAĐung");
            entity.Property(e => e.NoiDungCh)
                .HasMaxLength(500)
                .HasColumnName("NoiDungCH");
            entity.Property(e => e.Qthdid).HasColumnName("QTHDID");

            entity.HasOne(d => d.Qthd).WithMany(p => p.QtCauHoiQts)
                .HasForeignKey(d => d.Qthdid)
                .HasConstraintName("FK_QT_CauHoiQT_QT_NoiDungQT");
        });

        modelBuilder.Entity<QtCtbaiKiemTra>(entity =>
        {
            entity.HasKey(e => e.Idctkt);

            entity.ToTable("QT_CTBaiKiemTra");

            entity.Property(e => e.Idctkt).HasColumnName("IDCTKT");
            entity.Property(e => e.DapAnHv).HasColumnName("DapAnHV");
            entity.Property(e => e.IdcauHoi).HasColumnName("IDCauHoi");
            entity.Property(e => e.Iddađung)
                .HasMaxLength(10)
                .IsFixedLength()
                .HasColumnName("IDDAĐung");
            entity.Property(e => e.IdkiemTra).HasColumnName("IDKiemTra");

            entity.HasOne(d => d.IdkiemTraNavigation).WithMany(p => p.QtCtbaiKiemTras)
                .HasForeignKey(d => d.IdkiemTra)
                .HasConstraintName("FK_QT_CTBaiKiemTra_QT_BaiKiemTra");
        });

        modelBuilder.Entity<QtDinhKy>(entity =>
        {
            entity.HasKey(e => e.Iddk);

            entity.ToTable("QT_DinhKy");

            entity.Property(e => e.Iddk).HasColumnName("IDDK");
            entity.Property(e => e.TenDinhKy).HasMaxLength(50);
        });

        modelBuilder.Entity<QtFileQt>(entity =>
        {
            entity.HasKey(e => e.Idfile);

            entity.ToTable("QT_FileQT");

            entity.Property(e => e.Idfile).HasColumnName("IDFile");
            entity.Property(e => e.FilePdf).HasColumnName("FilePDF");
            entity.Property(e => e.OrderById).HasColumnName("OrderByID");
            entity.Property(e => e.Qthdid).HasColumnName("QTHDID");
            entity.Property(e => e.TenFile).HasMaxLength(250);

            entity.HasOne(d => d.Qthd).WithMany(p => p.QtFileQts)
                .HasForeignKey(d => d.Qthdid)
                .HasConstraintName("FK_QT_FileQT_QT_NoiDungQT");
        });

        modelBuilder.Entity<QtLoaiQt>(entity =>
        {
            entity.HasKey(e => e.Idloai);

            entity.ToTable("QT_LoaiQT");

            entity.Property(e => e.Idloai).HasColumnName("IDLoai");
            entity.Property(e => e.TenLoai).HasMaxLength(50);
        });

        modelBuilder.Entity<QtNoiDungQt>(entity =>
        {
            entity.HasKey(e => e.Idqthd).HasName("PK_QT_NoiDungQTHD");

            entity.ToTable("QT_NoiDungQT");

            entity.Property(e => e.Idqthd).HasColumnName("IDQTHD");
            entity.Property(e => e.IdloaiQthd).HasColumnName("IDLoaiQTHD");
            entity.Property(e => e.Idlvdt).HasColumnName("IDLVDT");
            entity.Property(e => e.IdphongBan).HasColumnName("IDPhongBan");
            entity.Property(e => e.MaHieu).HasMaxLength(50);
            entity.Property(e => e.NgayCapNhat).HasColumnType("smalldatetime");
            entity.Property(e => e.NoiDungCapNhat).HasMaxLength(250);
            entity.Property(e => e.TenQthd)
                .HasMaxLength(250)
                .HasColumnName("TenQTHD");

            entity.HasOne(d => d.IdloaiQthdNavigation).WithMany(p => p.QtNoiDungQts)
                .HasForeignKey(d => d.IdloaiQthd)
                .HasConstraintName("FK_QT_NoiDungQT_QT_LoaiQT");
        });

        modelBuilder.Entity<QtPhanQuyen>(entity =>
        {
            entity.HasKey(e => e.IdphanQuyen);

            entity.ToTable("QT_PhanQuyen");

            entity.Property(e => e.IdphanQuyen).HasColumnName("IDPhanQuyen");
            entity.Property(e => e.Dkid).HasColumnName("DKID");
            entity.Property(e => e.Idvtknl).HasColumnName("IDVTKNL");
            entity.Property(e => e.Qthdid).HasColumnName("QTHDID");

            entity.HasOne(d => d.Dk).WithMany(p => p.QtPhanQuyens)
                .HasForeignKey(d => d.Dkid)
                .HasConstraintName("FK_QT_PhanQuyen_QT_DinhKy");

            entity.HasOne(d => d.Qthd).WithMany(p => p.QtPhanQuyens)
                .HasForeignKey(d => d.Qthdid)
                .HasConstraintName("FK_QT_PhanQuyen_QT_NoiDungQT");
        });

        modelBuilder.Entity<QtVanBanLq>(entity =>
        {
            entity.HasKey(e => e.Idvb);

            entity.ToTable("QT_VanBanLQ");

            entity.Property(e => e.Idvb).HasColumnName("IDVB");
            entity.Property(e => e.IdqtLienQuan).HasColumnName("IDQT_LienQuan");
            entity.Property(e => e.Qthdid).HasColumnName("QTHDID");

            entity.HasOne(d => d.Qthd).WithMany(p => p.QtVanBanLqs)
                .HasForeignKey(d => d.Qthdid)
                .HasConstraintName("FK_QT_VanBanLQ_QT_NoiDungQT");
        });

        modelBuilder.Entity<Quyen>(entity =>
        {
            entity.HasKey(e => e.Idquyen);

            entity.ToTable("Quyen");

            entity.Property(e => e.Idquyen).HasColumnName("IDQuyen");
            entity.Property(e => e.TenQuyen).HasMaxLength(50);
        });

        modelBuilder.Entity<QuyenCn>(entity =>
        {
            entity.ToTable("QuyenCN");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.MaQuyen)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.TenQuyenCn)
                .HasMaxLength(50)
                .HasColumnName("TenQuyenCN");
        });

        modelBuilder.Entity<QuyenDetail>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Idcontroller).HasColumnName("IDController");
            entity.Property(e => e.Idquyen).HasColumnName("IDQuyen");
            entity.Property(e => e.IdquyenCn).HasColumnName("IDQuyenCN");
            entity.Property(e => e.IsActive).HasColumnName("isActive");
        });

        modelBuilder.Entity<TbKyLuat>(entity =>
        {
            entity.ToTable("TB_KyLuat");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.TbFile).HasColumnName("TB_File");
            entity.Property(e => e.TbNam).HasColumnName("TB_Nam");
            entity.Property(e => e.TbThang).HasColumnName("TB_Thang");
            entity.Property(e => e.TbTieuDe)
                .HasMaxLength(150)
                .HasColumnName("TB_TieuDe");
        });

        modelBuilder.Entity<ThongBao>(entity =>
        {
            entity.ToTable("ThongBao");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.NgayTb).HasColumnName("NgayTB");
            entity.Property(e => e.NoiDungTb).HasColumnName("NoiDungTB");
        });

        modelBuilder.Entity<TinhTrangLv>(entity =>
        {
            entity.ToTable("TinhTrangLV");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.TinhTrangLv1)
                .HasMaxLength(50)
                .HasColumnName("TinhTrangLV");
        });

        modelBuilder.Entity<ViTriKnl>(entity =>
        {
            entity.HasKey(e => e.Idvt);

            entity.ToTable("ViTriKNL");

            entity.Property(e => e.Idvt).HasColumnName("IDVT");
            entity.Property(e => e.Idkhoi).HasColumnName("IDKhoi");
            entity.Property(e => e.Idnhom).HasColumnName("IDNhom");
            entity.Property(e => e.Idpb).HasColumnName("IDPB");
            entity.Property(e => e.Idpx).HasColumnName("IDPX");
            entity.Property(e => e.Idto).HasColumnName("IDTo");
            entity.Property(e => e.Idvtparent).HasColumnName("IDVTParent");
            entity.Property(e => e.MaViTri).HasMaxLength(50);
            entity.Property(e => e.TenViTri).HasMaxLength(200);
        });

        modelBuilder.Entity<Vitri>(entity =>
        {
            entity.HasKey(e => e.IdviTri);

            entity.ToTable("Vitri");

            entity.Property(e => e.IdviTri).HasColumnName("IDViTri");
            entity.Property(e => e.TenViTri).HasMaxLength(250);
        });

        modelBuilder.Entity<XnhocTap>(entity =>
        {
            entity.HasKey(e => e.Idht).HasName("PK_XacNhanHocTap");

            entity.ToTable("XNHocTap");

            entity.Property(e => e.Idht).HasColumnName("IDHT");
            entity.Property(e => e.IdPhuongPhapDt).HasColumnName("ID_PhuongPhapDT");
            entity.Property(e => e.Idnd).HasColumnName("IDND");
            entity.Property(e => e.Lhid).HasColumnName("LHID");
            entity.Property(e => e.LyDoKhongTgia)
                .HasMaxLength(250)
                .HasColumnName("LyDoKhongTGia");
            entity.Property(e => e.NgayHt).HasColumnName("NgayHT");
            entity.Property(e => e.NgayTg).HasColumnName("NgayTG");
            entity.Property(e => e.Nvid).HasColumnName("NVID");
            entity.Property(e => e.Pbid).HasColumnName("PBID");
            entity.Property(e => e.Vtid).HasColumnName("VTID");
            entity.Property(e => e.Xnht).HasColumnName("XNHT");
            entity.Property(e => e.Xntg).HasColumnName("XNTG");

            entity.HasOne(d => d.Lh).WithMany(p => p.XnhocTaps)
                .HasForeignKey(d => d.Lhid)
                .HasConstraintName("FK_XNHocTap_LopHoc");

            entity.HasOne(d => d.Nv).WithMany(p => p.XnhocTaps)
                .HasForeignKey(d => d.Nvid)
                .HasConstraintName("FK_XNHocTap_NhanVien");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
