import Foundation
import ImageIO
import UniformTypeIdentifiers

enum PPTXError: LocalizedError {
    case tool(String)
    case image(String)
    var errorDescription: String? {
        switch self {
        case .tool(let s): return s
        case .image(let s): return "画像を処理できません: \(s)"
        }
    }
}

struct SlideFonts: Equatable {
    var latin: String
    var ea: String
    /// テンプレートの既定
    static let template = SlideFonts(latin: "Calibri", ea: "ＭＳ Ｐゴシック")
    /// アプリの初期値(欧文はArial、和文はテンプレートのまま)
    static let `default` = SlideFonts(latin: "Arial", ea: template.ea)
}

/// 情報欄のデザイン。forum = 元のフォーラムのテンプレ(焦点距離を中央に大きく)、
/// compact = カメラ名を大きく・レンズ名と露出を小さくまとめた形(2段組みと同じ構成)
enum SlideLayout: String, CaseIterable, Identifiable {
    case forum, compact
    var id: String { rawValue }
    var label: String {
        switch self {
        case .forum: return "フォーラム標準"
        case .compact: return "コンパクト"
        }
    }

    func fontSize(_ k: FieldKind) -> Double {
        guard self == .compact else { return k.fontSize }
        switch k {
        case .camera: return 12
        case .lens, .focal, .shutter, .aperture, .iso, .note: return 7
        default: return k.fontSize
        }
    }
}

/// スライドの背景色と、それに合わせた文字色
struct SlideStyle: Equatable {
    var bgHex: String
    static let black = SlideStyle(bgHex: "000000")
    static let lightGray = SlideStyle(bgHex: "F2F2F2")

    /// 背景が明るければ黒、暗ければ白(相対輝度で判定)
    var textHex: String {
        let v = Int(bgHex, radix: 16) ?? 0
        let c = [(v >> 16) & 255, (v >> 8) & 255, v & 255].map { Double($0) / 255 }
        return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2] > 0.5 ? "000000" : "FFFFFF"
    }
}

/// スライド上の写真1枚分の領域(写真ボックスと、その右隣の情報列)
struct Panel {
    var photoX: Int, photoMaxW: Int
    var colX: Int, colW: Int
    var narrow: Bool
    // 狭い列はテンプレートの寸法に合わせる: 文字は列の左端+7.2pt(既定の余白)から、赤線は列の左端+83219、幅933061
    var lowerX: Int { narrow ? colX + 55440 : colX + 184224 }
    var lowerW: Int { narrow ? colW - 55440 : 1326277 }
    var lineX: Int { narrow ? colX + 83219 : 7817723 }
    var lineW: Int { narrow ? 933061 : 1215730 }
    /// カメラ名・レンズ名のボックス(コンパクト用。赤線の中央に揃う)
    var camX: Int { colX + 105331 }
    var camW: Int { 933061 }
    /// 撮影情報ボックスの左右余白(狭い列では詰めて「Shutter Speed: 1/800 sec」を1行に収める)
    var lowerInsets: (l: Int, r: Int) { narrow ? (36000, 0) : (91440, 91440) }

    static func single(_ layout: SlideLayout) -> Panel {
        layout == .forum
            ? Panel(photoX: 0, photoMaxW: PPTXBuilder.photoMaxW, colX: PPTXBuilder.colX, colW: PPTXBuilder.colW, narrow: false)
            : Panel(photoX: 0, photoMaxW: 7717561, colX: 8047536, colW: 1143722, narrow: true)
    }
    /// 2段組み(左・右)。右側は右端の列に学籍番号・氏名が入る
    static let left = Panel(photoX: 0, photoMaxW: 3473576, colX: 3453800, colW: 1143722, narrow: true)
    static let right = Panel(photoX: 4573960, photoMaxW: 3473576, colX: 8028384, colW: 1143722, narrow: true)
}

enum PPTXBuilder {
    // テンプレートの寸法 (EMU)
    static let slideH = 5143500
    static let photoMaxW = 7721508          // 写真領域の最大幅(右カラムに食い込まない)
    static let colX = 7633500, colW = 1584176
    static let startY = 180783

    static func build(template: URL, output: URL, photos: [Photo], fields: [FieldSetting],
                      studentID: String, name: String, fonts: SlideFonts = .default, lineSpacing: Double = 1.0, style: SlideStyle = .black, layout: SlideLayout = .forum) throws {
        let fm = FileManager.default
        let work = fm.temporaryDirectory.appendingPathComponent("pfp-\(UUID().uuidString)")
        defer { try? fm.removeItem(at: work) }
        try fm.createDirectory(at: work, withIntermediateDirectories: true)
        let pkg = work.appendingPathComponent("pkg")
        try run("/usr/bin/unzip", ["-q", template.path, "-d", pkg.path])

        // テンプレート内の既存スライド・変更履歴を撤去
        for d in ["ppt/slides", "ppt/notesSlides", "ppt/changesInfos"] {
            try? fm.removeItem(at: pkg.appendingPathComponent(d))
        }
        try? fm.removeItem(at: pkg.appendingPathComponent("ppt/revisionInfo.xml"))
        for m in ["image3.jpeg", "image4.jpeg"] {
            try? fm.removeItem(at: pkg.appendingPathComponent("ppt/media/\(m)"))
        }
        try fm.createDirectory(at: pkg.appendingPathComponent("ppt/slides/_rels"), withIntermediateDirectories: true)

        var overrides = "", presRels = "", sldIds = ""
        for (i, group) in slideGroups(photos).enumerated() {
            let n = i + 1
            var rels = "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideLayout\" Target=\"../slideLayouts/slideLayout37.xml\"/>"
            for (k, photo) in group.enumerated() {
                let imgName = "photo\(n)_\(k + 1).jpeg"
                try writeJPEG(photo, to: pkg.appendingPathComponent("ppt/media/\(imgName)"))
                rels += "<Relationship Id=\"rId\(k + 2)\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/image\" Target=\"../media/\(imgName)\"/>"
            }
            let xml = slideXML(photos: group, fields: fields, studentID: studentID, name: name, fonts: fonts, lineSpacing: lineSpacing, style: style, layout: layout)
            try xml.write(to: pkg.appendingPathComponent("ppt/slides/slide\(n).xml"), atomically: true, encoding: .utf8)
            let relsXML = """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">\(rels)</Relationships>
            """
            try relsXML.write(to: pkg.appendingPathComponent("ppt/slides/_rels/slide\(n).xml.rels"), atomically: true, encoding: .utf8)
            overrides += "<Override PartName=\"/ppt/slides/slide\(n).xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.slide+xml\"/>"
            presRels += "<Relationship Id=\"rIdPF\(n)\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/slide\" Target=\"slides/slide\(n).xml\"/>"
            sldIds += "<p:sldId id=\"\(255 + n)\" r:id=\"rIdPF\(n)\"/>"
        }

        try edit(pkg.appendingPathComponent("[Content_Types].xml")) { s in
            s = s.replacingOccurrences(of: #"<Override PartName="/ppt/(slides|notesSlides|changesInfos)/[^"]*"[^>]*/>"#, with: "", options: .regularExpression)
            s = s.replacingOccurrences(of: #"<Override PartName="/ppt/revisionInfo.xml"[^>]*/>"#, with: "", options: .regularExpression)
            s = s.replacingOccurrences(of: "</Types>", with: overrides + "</Types>")
        }
        try edit(pkg.appendingPathComponent("ppt/_rels/presentation.xml.rels")) { s in
            s = s.replacingOccurrences(of: #"<Relationship [^>]*Target="(slides/|changesInfos/|revisionInfo)[^"]*"[^>]*/>"#, with: "", options: .regularExpression)
            s = s.replacingOccurrences(of: "</Relationships>", with: presRels + "</Relationships>")
        }
        try edit(pkg.appendingPathComponent("ppt/presentation.xml")) { s in
            s = s.replacingOccurrences(of: #"<p:sldIdLst>.*?</p:sldIdLst>"#, with: "<p:sldIdLst>\(sldIds)</p:sldIdLst>", options: .regularExpression)
        }

        try? fm.removeItem(at: output)
        try run("/bin/sh", ["-c", "cd \"$1\" && /usr/bin/zip -qr -X \"$2\" '[Content_Types].xml' . -x '[Content_Types].xml'", "sh", pkg.path, output.path])
    }

    // MARK: - スライド生成

    /// 「次の写真と並べる」が付いた写真を次の写真と同じスライドにする(最大2枚)
    static func slideGroups(_ photos: [Photo]) -> [[Photo]] {
        var out: [[Photo]] = [], i = 0
        while i < photos.count {
            if photos[i].pairWithNext, i + 1 < photos.count { out.append([photos[i], photos[i + 1]]); i += 2 }
            else { out.append([photos[i]]); i += 1 }
        }
        return out
    }

    static func slideXML(photos: [Photo], fields: [FieldSetting], studentID: String, name: String, fonts: SlideFonts,
                         lineSpacing: Double = 1.0, style: SlideStyle = .black, layout: SlideLayout = .forum) -> String {
        let panels: [Panel] = photos.count == 2 ? [.left, .right] : [.single(layout)]
        var shapes = "", id = 10
        for (i, photo) in photos.enumerated() {
            let panel = panels[i]
            shapes += pictureShape(id: 2 + i, rel: "rId\(i + 2)", photo: photo, panel: panel)
            shapes += panelShapes(photo: photo, panel: panel, identity: i == photos.count - 1, fields: fields,
                                  studentID: studentID, name: name, fonts: fonts, lineSpacing: lineSpacing, style: style, layout: layout, id: &id)
        }
        return """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <p:sld xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main"><p:cSld><p:bg><p:bgPr><a:solidFill><a:srgbClr val="\(style.bgHex)"/></a:solidFill><a:effectLst/></p:bgPr></p:bg><p:spTree><p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/><a:chOff x="0" y="0"/><a:chExt cx="0" cy="0"/></a:xfrm></p:grpSpPr>\(shapes)</p:spTree></p:cSld><p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr></p:sld>
        """
    }

    /// 写真: 領域の左寄せ・高さいっぱい。横長すぎて領域の幅を超える場合のみ幅に合わせて縮小
    static func pictureShape(id: Int, rel: String, photo: Photo, panel: Panel) -> String {
        let aspect = photo.pixelSize.width / photo.pixelSize.height
        var w = Int((Double(slideH) * aspect).rounded()), h = slideH
        if w > panel.photoMaxW { w = panel.photoMaxW; h = Int((Double(panel.photoMaxW) / aspect).rounded()) }
        let y = (slideH - h) / 2
        return """
        <p:pic><p:nvPicPr><p:cNvPr id="\(id)" name="写真 \(id - 1)"/><p:cNvPicPr><a:picLocks noChangeAspect="1"/></p:cNvPicPr><p:nvPr/></p:nvPicPr><p:blipFill><a:blip r:embed="\(rel)"/><a:stretch><a:fillRect/></a:stretch></p:blipFill><p:spPr><a:xfrm><a:off x="\(panel.photoX)" y="\(y)"/><a:ext cx="\(w)" cy="\(h)"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom></p:spPr></p:pic>
        """
    }

    /// 写真1枚ぶんの情報列。個人情報(学籍番号/氏名/タイトル)は上から順に積み、
    /// 撮影情報はテンプレート通り赤線を境に下部へ固定する(カメラ・レンズが線の上、露出が線の下)。
    /// identity=false の列には学籍番号・氏名を出さない(2段組みの左側)
    static func panelShapes(photo: Photo, panel: Panel, identity: Bool, fields: [FieldSetting], studentID: String, name: String,
                            fonts: SlideFonts, lineSpacing: Double, style: SlideStyle, layout: SlideLayout, id: inout Int) -> String {
        if layout == .compact {
            return compactPanelShapes(photo: photo, panel: panel, identity: identity, fields: fields, studentID: studentID, name: name,
                                      fonts: fonts, lineSpacing: lineSpacing, style: style, id: &id)
        }
        var shapes = ""
        func height(_ k: FieldKind, _ text: String) -> Int {
            Int(Double(estimateLines(text, size: k.fontSize, width: panel.colW)) * k.fontSize * 1.2 * 12700) + 91440
        }
        func emit(_ k: FieldKind, _ text: String, y: Int) {
            shapes += textShape(id: id, y: y, h: height(k, text), text: text, size: k.fontSize, x: panel.colX, w: panel.colW, fonts: fonts, color: style.textHex); id += 1
        }
        let items: [(FieldKind, String)] = fields.filter(\.enabled).compactMap { f in
            if !identity && (f.kind == .studentID || f.kind == .name) { return nil }
            return lineText(f.kind, photo: photo, studentID: studentID, name: name).flatMap { $0.isEmpty ? nil : (f.kind, $0) }
        }
        let inset = 45720                                   // テキストボックスの上下余白
        func step(_ k: FieldKind, _ t: String) -> Int { height(k, t) - 2 * inset }   // 本文の高さ分だけ送る

        var cursor = startY
        for (k, t) in items where k.group == 0 {
            emit(k, t, y: cursor); cursor += step(k, t)
        }

        // 焦点距離: 情報列の中央に大きく(完成図の位置)
        if let (k, t) = items.first(where: { $0.0.group == 3 }) {
            emit(k, t, y: 2744000 - height(k, t) / 2)
        }

        let upper = items.filter { $0.0.group == 1 }, lower = items.filter { $0.0.group == 2 }
        if !upper.isEmpty || !lower.isEmpty {
            let lineY = 4412530
            shapes += lineShape(id: id, y: lineY, x: panel.lineX, w: panel.lineW); id += 1
            var edge = lineY - 25000                     // 本文の下端(線の少し上)
            for (k, t) in upper.reversed() {
                let y = edge + inset - height(k, t)
                emit(k, t, y: y); edge = y + inset
            }
            let down = lineY + 30000 - inset
            if !lower.isEmpty {                          // 線の下は1つのテキストボックスにまとめ、左寄せ
                let h = lowerHeight(lower, spacing: lineSpacing, width: panel.lowerW, pad: Double(panel.lowerInsets.l + panel.lowerInsets.r) / 12700)
                shapes += lowerShape(id: id, y: down, h: h, items: lower, x: panel.lowerX, w: panel.lowerW, insets: panel.lowerInsets, fonts: fonts, spacing: lineSpacing, color: style.textHex); id += 1
            }
        }
        return shapes
    }

    /// コンパクト: 個人情報は上、カメラ名(12pt)→レンズ名(7pt)→赤線→SS/焦点距離/F値/ISO(7pt・左寄せ)を下部にまとめる
    static func compactPanelShapes(photo: Photo, panel: Panel, identity: Bool, fields: [FieldSetting], studentID: String, name: String,
                                   fonts: SlideFonts, lineSpacing: Double, style: SlideStyle, id: inout Int) -> String {
        let layout = SlideLayout.compact
        var shapes = ""
        func lines(_ k: FieldKind, _ t: String) -> Int { estimateLines(t, size: layout.fontSize(k), width: panel.colW) }
        func body(_ k: FieldKind, _ t: String) -> Double { Double(lines(k, t)) * layout.fontSize(k) * 1.2 * 12700 }
        func emit(_ k: FieldKind, _ t: String, y: Int) {
            shapes += textShape(id: id, y: y, h: Int(body(k, t)) + 91440, text: t, size: layout.fontSize(k), x: panel.colX, w: panel.colW, fonts: fonts, color: style.textHex); id += 1
        }
        func text(_ k: FieldKind) -> String? {
            guard fields.first(where: { $0.kind == k })?.enabled == true else { return nil }
            if !identity && (k == .studentID || k == .name) { return nil }
            return lineText(k, photo: photo, studentID: studentID, name: name, layout: layout).flatMap { $0.isEmpty ? nil : $0 }
        }

        var cursor = startY                                   // 上: 学籍番号・氏名・タイトル
        for k in [FieldKind.studentID, .name, .title] {
            if let t = text(k) { emit(k, t, y: cursor); cursor += Int(body(k, t)) }
        }

        let lineY = 4507663
        let lensLines = (text(.lens) ?? "").split(separator: "\n", omittingEmptySubsequences: true).map(String.init)
        let camera = text(.camera)
        let list: [(FieldKind, String)] = [FieldKind.shutter, .focal, .aperture, .iso, .note].compactMap { k in text(k).map { (k, $0) } }
        if !lensLines.isEmpty || camera != nil || !list.isEmpty {
            shapes += lineShape(id: id, y: lineY, x: panel.lineX, w: panel.lineW); id += 1
        }
        let lensBottom = lineY + 10000                         // 線の上へ積み上げる(レンズ → カメラ)
        var cameraBottom = lensBottom
        if !lensLines.isEmpty {
            let lensBody = Double(lensLines.reduce(0) { $0 + estimateLines($1, size: layout.fontSize(.lens), width: panel.camW) }) * layout.fontSize(.lens) * 1.2 * 12700
            let y = lensBottom - Int(lensBody) - 91440
            shapes += lensShape(id: id, y: y, h: Int(lensBody) + 91440, lines: lensLines, x: panel.camX, w: panel.camW, fonts: fonts, color: style.textHex); id += 1
            cameraBottom = y + 105000
        }
        if let t = camera {
            let n = estimateLines(t, size: layout.fontSize(.camera), width: panel.camW)
            let body = Double(n) * layout.fontSize(.camera) * 1.2 * 12700
            let y = cameraBottom - Int(body) - 91440 - 14000
            shapes += textShape(id: id, y: y, h: Int(body) + 91440, text: t, size: layout.fontSize(.camera), x: panel.camX, w: panel.camW, fonts: fonts, color: style.textHex); id += 1
        }
        if !list.isEmpty {
            let h = Int(list.reduce(0.0) { $0 + body($1.0, $1.1) * lineSpacing }) + 91440
            shapes += lowerShape(id: id, y: 4525086, h: h, items: list, x: panel.lowerX, w: panel.lowerW, insets: panel.lowerInsets,
                                 fonts: fonts, spacing: lineSpacing, color: style.textHex, layout: layout); id += 1
        }
        return shapes
    }

    static func lineText(_ k: FieldKind, photo: Photo, studentID: String, name: String, layout: SlideLayout = .forum) -> String? {
        func v(_ s: String) -> String? { s.trimmingCharacters(in: .whitespaces).isEmpty ? nil : s.trimmingCharacters(in: .whitespaces) }
        switch k {
        case .studentID: return v(studentID)
        case .name: return v(name)
        case .title:
            guard let t = v(photo.title) else { return nil }
            return t.hasPrefix("「") ? t : "「\(t)」"
        case .camera: return v(photo.camera)
        case .lens: return layout == .compact ? v(photo.lens) : v(photo.lens).map { $0.replacingOccurrences(of: "\n", with: " ") }
        case .focal: return v(photo.focal).map { layout == .compact ? "Focal Length: \($0)mm" : "\($0)mm" }
        case .shutter: return v(photo.shutter).map { "Shutter Speed: \($0) sec" }
        case .aperture: return v(photo.aperture).map { "Aperture: f/\($0)" }
        case .iso: return v(photo.iso).map { "ISO: \($0)" }
        case .note: return v(photo.note)
        }
    }

    static func lowerHeight(_ items: [(FieldKind, String)], spacing: Double, width: Int, pad: Double) -> Int {
        let body = items.reduce(0.0) { $0 + Double(estimateLines($1.1, size: $1.0.fontSize, width: width, pad: pad)) * $1.0.fontSize * 1.2 * spacing }
        return Int(body * 12700) + 91440
    }

    /// 撮影情報(シャッタースピード/F値/ISO/備考)を1つのテキストボックスにまとめる。行間は spacing(1.0 = 100%)
    static func lowerShape(id: Int, y: Int, h: Int, items: [(FieldKind, String)], x: Int, w: Int, insets: (l: Int, r: Int), fonts: SlideFonts, spacing: Double, color: String, layout: SlideLayout = .forum) -> String {
        let paras = items.map { k, t -> String in
            let sz = Int(layout.fontSize(k) * 100)
            let ppr = "<a:pPr algn=\"l\"\(k == .note ? " marL=\"54000\"" : "")><a:lnSpc><a:spcPct val=\"\(Int((spacing * 100000).rounded()))\"/></a:lnSpc></a:pPr>"
            return "<a:p>\(ppr)<a:r><a:rPr lang=\"ja-JP\" altLang=\"en-US\" sz=\"\(sz)\" dirty=\"0\"><a:solidFill><a:srgbClr val=\"\(color)\"/></a:solidFill><a:latin typeface=\"\(esc(fonts.latin))\"/><a:ea typeface=\"\(esc(fonts.ea))\"/></a:rPr><a:t>\(esc(t))</a:t></a:r></a:p>"
        }.joined()
        return """
        <p:sp><p:nvSpPr><p:cNvPr id="\(id)" name="撮影情報"/><p:cNvSpPr txBox="1"/><p:nvPr/></p:nvSpPr><p:spPr><a:xfrm><a:off x="\(x)" y="\(y)"/><a:ext cx="\(w)" cy="\(h)"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:noFill/></p:spPr><p:txBody><a:bodyPr wrap="square" lIns="\(insets.l)" rIns="\(insets.r)" rtlCol="0"><a:spAutoFit/></a:bodyPr><a:lstStyle/>\(paras)</p:txBody></p:sp>
        """
    }

    static func estimateLines(_ s: String, size: Double, width colWidth: Int = colW, pad: Double = 14.4) -> Int {
        let avail = Double(colWidth) / 12700 - pad
        var width = 0.0, lines = 1.0
        for ch in s.unicodeScalars {
            let cw = ch.value < 0x2000 ? size * 0.52 : size
            if width + cw > avail { lines += 1; width = 0 }
            width += cw
        }
        return Int(lines)
    }

    static func esc(_ s: String) -> String {
        s.replacingOccurrences(of: "&", with: "&amp;").replacingOccurrences(of: "<", with: "&lt;").replacingOccurrences(of: ">", with: "&gt;")
    }

    static func textShape(id: Int, y: Int, h: Int, text: String, size: Double, x: Int, w: Int, fonts: SlideFonts = .default, color: String = "FFFFFF") -> String {
        let sz = Int(size * 100)
        return """
        <p:sp><p:nvSpPr><p:cNvPr id="\(id)" name="テキスト \(id)"/><p:cNvSpPr txBox="1"/><p:nvPr/></p:nvSpPr><p:spPr><a:xfrm><a:off x="\(x)" y="\(y)"/><a:ext cx="\(w)" cy="\(h)"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:noFill/></p:spPr><p:txBody><a:bodyPr wrap="square" rtlCol="0"><a:spAutoFit/></a:bodyPr><a:lstStyle/><a:p><a:pPr algn="ctr"/><a:r><a:rPr lang="ja-JP" altLang="en-US" sz="\(sz)" dirty="0"><a:solidFill><a:srgbClr val="\(color)"/></a:solidFill><a:latin typeface="\(esc(fonts.latin))"/><a:ea typeface="\(esc(fonts.ea))"/></a:rPr><a:t>\(esc(text))</a:t></a:r></a:p></p:txBody></p:sp>
        """
    }

    /// レンズ名のボックス。1行なら中央、2行以上なら 1行目を左寄せ・最後の行を右寄せ(間は左寄せ)
    static func lensShape(id: Int, y: Int, h: Int, lines: [String], x: Int, w: Int, fonts: SlideFonts, color: String) -> String {
        let sz = Int(SlideLayout.compact.fontSize(.lens) * 100)
        let paras = lines.enumerated().map { i, t -> String in
            let algn = lines.count == 1 ? "ctr" : (i == lines.count - 1 ? "r" : "l")
            return "<a:p><a:pPr algn=\"\(algn)\"/><a:r><a:rPr lang=\"ja-JP\" altLang=\"en-US\" sz=\"\(sz)\" dirty=\"0\"><a:solidFill><a:srgbClr val=\"\(color)\"/></a:solidFill><a:latin typeface=\"\(esc(fonts.latin))\"/><a:ea typeface=\"\(esc(fonts.ea))\"/></a:rPr><a:t>\(esc(t))</a:t></a:r></a:p>"
        }.joined()
        return """
        <p:sp><p:nvSpPr><p:cNvPr id="\(id)" name="レンズ名"/><p:cNvSpPr txBox="1"/><p:nvPr/></p:nvSpPr><p:spPr><a:xfrm><a:off x="\(x)" y="\(y)"/><a:ext cx="\(w)" cy="\(h)"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:noFill/></p:spPr><p:txBody><a:bodyPr wrap="square" rtlCol="0"><a:spAutoFit/></a:bodyPr><a:lstStyle/>\(paras)</p:txBody></p:sp>
        """
    }

    static func lineShape(id: Int, y: Int, x: Int, w: Int) -> String {
        """
        <p:cxnSp><p:nvCxnSpPr><p:cNvPr id="\(id)" name="直線 \(id)"/><p:cNvCxnSpPr/><p:nvPr/></p:nvCxnSpPr><p:spPr><a:xfrm><a:off x="\(x)" y="\(y)"/><a:ext cx="\(w)" cy="0"/></a:xfrm><a:prstGeom prst="line"><a:avLst/></a:prstGeom><a:ln><a:solidFill><a:srgbClr val="FF6600"/></a:solidFill></a:ln></p:spPr></p:cxnSp>
        """
    }

    // MARK: - 補助

    /// JPEGで向き補正が不要ならそのまま、それ以外(HEIC/RAW/PNG/回転あり)は向きを焼き込んでJPEG化
    static func writeJPEG(_ photo: Photo, to dest: URL) throws {
        let ext = photo.url.pathExtension.lowercased()
        if (ext == "jpg" || ext == "jpeg") && photo.orientation == 1 {
            try FileManager.default.copyItem(at: photo.url, to: dest)
            return
        }
        guard let src = CGImageSourceCreateWithURL(photo.url as CFURL, nil) else { throw PPTXError.image(photo.url.lastPathComponent) }
        let opts: [CFString: Any] = [
            kCGImageSourceCreateThumbnailFromImageAlways: true,
            kCGImageSourceCreateThumbnailWithTransform: true,
            kCGImageSourceThumbnailMaxPixelSize: max(photo.pixelSize.width, photo.pixelSize.height),
        ]
        guard let cg = CGImageSourceCreateThumbnailAtIndex(src, 0, opts as CFDictionary),
              let d = CGImageDestinationCreateWithURL(dest as CFURL, UTType.jpeg.identifier as CFString, 1, nil)
        else { throw PPTXError.image(photo.url.lastPathComponent) }
        CGImageDestinationAddImage(d, cg, [kCGImageDestinationLossyCompressionQuality: 0.95] as CFDictionary)
        guard CGImageDestinationFinalize(d) else { throw PPTXError.image(photo.url.lastPathComponent) }
    }

    static func edit(_ url: URL, _ f: (inout String) -> Void) throws {
        var s = try String(contentsOf: url, encoding: .utf8)
        f(&s)
        try s.write(to: url, atomically: true, encoding: .utf8)
    }

    static func run(_ path: String, _ args: [String]) throws {
        let p = Process()
        p.executableURL = URL(fileURLWithPath: path)
        p.arguments = args
        let err = Pipe()
        p.standardError = err
        try p.run()
        p.waitUntilExit()
        if p.terminationStatus != 0 {
            let msg = String(data: err.fileHandleForReading.readDataToEndOfFile(), encoding: .utf8) ?? ""
            throw PPTXError.tool("\(path) 失敗: \(msg)")
        }
    }
}
