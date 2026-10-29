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

enum PPTXBuilder {
    // テンプレートの寸法 (EMU)
    static let slideH = 5143500
    static let photoMaxW = 7721508          // 写真領域の最大幅(右カラムに食い込まない)
    static let colX = 7633500, colW = 1584176
    static let startY = 180783

    static func build(template: URL, output: URL, photos: [Photo], fields: [FieldSetting],
                      studentID: String, name: String, fonts: SlideFonts = .default, lineSpacing: Double = 1.0) throws {
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
        for (i, photo) in photos.enumerated() {
            let n = i + 1
            let imgName = "photo\(n).jpeg"
            try writeJPEG(photo, to: pkg.appendingPathComponent("ppt/media/\(imgName)"))
            let xml = slideXML(photo: photo, fields: fields, studentID: studentID, name: name, fonts: fonts, lineSpacing: lineSpacing)
            try xml.write(to: pkg.appendingPathComponent("ppt/slides/slide\(n).xml"), atomically: true, encoding: .utf8)
            let rels = """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideLayout" Target="../slideLayouts/slideLayout37.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="../media/\(imgName)"/></Relationships>
            """
            try rels.write(to: pkg.appendingPathComponent("ppt/slides/_rels/slide\(n).xml.rels"), atomically: true, encoding: .utf8)
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

    static func slideXML(photo: Photo, fields: [FieldSetting], studentID: String, name: String, fonts: SlideFonts, lineSpacing: Double = 1.0) -> String {
        // 写真: 左寄せ・高さいっぱい。横長すぎて右カラムに掛かる場合のみ幅に合わせて縮小
        let aspect = photo.pixelSize.width / photo.pixelSize.height
        var w = Int((Double(slideH) * aspect).rounded()), h = slideH
        if w > photoMaxW { w = photoMaxW; h = Int((Double(photoMaxW) / aspect).rounded()) }
        let y = (slideH - h) / 2

        var shapes = """
        <p:pic><p:nvPicPr><p:cNvPr id="2" name="写真"/><p:cNvPicPr><a:picLocks noChangeAspect="1"/></p:cNvPicPr><p:nvPr/></p:nvPicPr><p:blipFill><a:blip r:embed="rId2"/><a:stretch><a:fillRect/></a:stretch></p:blipFill><p:spPr><a:xfrm><a:off x="0" y="\(y)"/><a:ext cx="\(w)" cy="\(h)"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom></p:spPr></p:pic>
        """

        // 右カラム: 個人情報(学籍番号/氏名/タイトル)は上から順に積み、
        // 撮影情報はテンプレート通り赤線を境に下部へ固定する(カメラ・レンズが線の上、露出が線の下)
        var id = 10
        func height(_ k: FieldKind, _ text: String) -> Int {
            Int(Double(estimateLines(text, size: k.fontSize)) * k.fontSize * 1.2 * 12700) + 91440
        }
        func emit(_ k: FieldKind, _ text: String, y: Int, left: Bool = false) {
            shapes += textShape(id: id, y: y, h: height(k, text), text: text, size: k.fontSize, left: left, indent: k == .note, fonts: fonts); id += 1
        }
        let items: [(FieldKind, String)] = fields.filter(\.enabled).compactMap { f in
            lineText(f.kind, photo: photo, studentID: studentID, name: name).flatMap { $0.isEmpty ? nil : (f.kind, $0) }
        }
        let inset = 45720                                   // テキストボックスの上下余白
        func step(_ k: FieldKind, _ t: String) -> Int { height(k, t) - 2 * inset }   // 本文の高さ分だけ送る

        var cursor = startY
        for (k, t) in items where k.group == 0 {
            emit(k, t, y: cursor); cursor += step(k, t)
        }

        // 焦点距離: 右カラム中央に大きく(完成図の位置)
        if let (k, t) = items.first(where: { $0.0.group == 3 }) {
            emit(k, t, y: 2744000 - height(k, t) / 2)
        }

        let upper = items.filter { $0.0.group == 1 }, lower = items.filter { $0.0.group == 2 }
        if !upper.isEmpty || !lower.isEmpty {
            let lineY = 4412530
            shapes += lineShape(id: id, y: lineY); id += 1
            var edge = lineY - 25000                     // 本文の下端(線の少し上)
            for (k, t) in upper.reversed() {
                let y = edge + inset - height(k, t)
                emit(k, t, y: y); edge = y + inset
            }
            let down = lineY + 30000 - inset
            if !lower.isEmpty {                          // 線の下は1つのテキストボックスにまとめ、左寄せ
                let h = lowerHeight(lower, spacing: lineSpacing)
                shapes += lowerShape(id: id, y: down, h: h, items: lower, fonts: fonts, spacing: lineSpacing); id += 1
            }
        }

        return """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <p:sld xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main"><p:cSld><p:spTree><p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/><a:chOff x="0" y="0"/><a:chExt cx="0" cy="0"/></a:xfrm></p:grpSpPr>\(shapes)</p:spTree></p:cSld><p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr></p:sld>
        """
    }

    static func lineText(_ k: FieldKind, photo: Photo, studentID: String, name: String) -> String? {
        func v(_ s: String) -> String? { s.trimmingCharacters(in: .whitespaces).isEmpty ? nil : s.trimmingCharacters(in: .whitespaces) }
        switch k {
        case .studentID: return v(studentID)
        case .name: return v(name)
        case .title:
            guard let t = v(photo.title) else { return nil }
            return t.hasPrefix("「") ? t : "「\(t)」"
        case .camera: return v(photo.camera)
        case .lens: return v(photo.lens)
        case .focal: return v(photo.focal).map { "\($0)mm" }
        case .shutter: return v(photo.shutter).map { "Shutter Speed: \($0) sec" }
        case .aperture: return v(photo.aperture).map { "Aperture: f/\($0)" }
        case .iso: return v(photo.iso).map { "ISO: \($0)" }
        case .note: return v(photo.note)
        }
    }

    static let lowerX = 7817724, lowerW = 1326277

    static func lowerHeight(_ items: [(FieldKind, String)], spacing: Double) -> Int {
        let body = items.reduce(0.0) { $0 + Double(estimateLines($1.1, size: $1.0.fontSize, width: lowerW)) * $1.0.fontSize * 1.2 * spacing }
        return Int(body * 12700) + 91440
    }

    /// 撮影情報(シャッタースピード/F値/ISO/備考)を1つのテキストボックスにまとめる。行間は spacing(1.0 = 100%)
    static func lowerShape(id: Int, y: Int, h: Int, items: [(FieldKind, String)], fonts: SlideFonts, spacing: Double) -> String {
        let paras = items.map { k, t -> String in
            let sz = Int(k.fontSize * 100)
            let ppr = "<a:pPr algn=\"l\"\(k == .note ? " marL=\"54000\"" : "")><a:lnSpc><a:spcPct val=\"\(Int((spacing * 100000).rounded()))\"/></a:lnSpc></a:pPr>"
            return "<a:p>\(ppr)<a:r><a:rPr lang=\"ja-JP\" altLang=\"en-US\" sz=\"\(sz)\" dirty=\"0\"><a:solidFill><a:prstClr val=\"white\"/></a:solidFill><a:latin typeface=\"\(esc(fonts.latin))\"/><a:ea typeface=\"\(esc(fonts.ea))\"/></a:rPr><a:t>\(esc(t))</a:t></a:r></a:p>"
        }.joined()
        return """
        <p:sp><p:nvSpPr><p:cNvPr id="\(id)" name="撮影情報"/><p:cNvSpPr txBox="1"/><p:nvPr/></p:nvSpPr><p:spPr><a:xfrm><a:off x="\(lowerX)" y="\(y)"/><a:ext cx="\(lowerW)" cy="\(h)"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:noFill/></p:spPr><p:txBody><a:bodyPr wrap="square" rtlCol="0"><a:spAutoFit/></a:bodyPr><a:lstStyle/>\(paras)</p:txBody></p:sp>
        """
    }

    static func estimateLines(_ s: String, size: Double, width colWidth: Int = colW) -> Int {
        let avail = Double(colWidth) / 12700 - 14.4
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

    static func textShape(id: Int, y: Int, h: Int, text: String, size: Double, left: Bool = false, indent: Bool = false, fonts: SlideFonts = .default) -> String {
        let sz = Int(size * 100)
        // 左寄せはテンプレートの露出情報ボックスと同じ位置・幅(赤線の左端に文字頭を揃える)
        let x = left ? 7817724 : colX, w = left ? 1326277 : colW
        let ppr = left ? "<a:pPr algn=\"l\"\(indent ? " marL=\"54000\"" : "")/>" : "<a:pPr algn=\"ctr\"/>"
        return """
        <p:sp><p:nvSpPr><p:cNvPr id="\(id)" name="テキスト \(id)"/><p:cNvSpPr txBox="1"/><p:nvPr/></p:nvSpPr><p:spPr><a:xfrm><a:off x="\(x)" y="\(y)"/><a:ext cx="\(w)" cy="\(h)"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:noFill/></p:spPr><p:txBody><a:bodyPr wrap="square" rtlCol="0"><a:spAutoFit/></a:bodyPr><a:lstStyle/><a:p>\(ppr)<a:r><a:rPr lang="ja-JP" altLang="en-US" sz="\(sz)" dirty="0"><a:solidFill><a:prstClr val="white"/></a:solidFill><a:latin typeface="\(esc(fonts.latin))"/><a:ea typeface="\(esc(fonts.ea))"/></a:rPr><a:t>\(esc(text))</a:t></a:r></a:p></p:txBody></p:sp>
        """
    }

    static func lineShape(id: Int, y: Int) -> String {
        """
        <p:cxnSp><p:nvCxnSpPr><p:cNvPr id="\(id)" name="直線 \(id)"/><p:cNvCxnSpPr/><p:nvPr/></p:nvCxnSpPr><p:spPr><a:xfrm><a:off x="7817723" y="\(y)"/><a:ext cx="1215730" cy="0"/></a:xfrm><a:prstGeom prst="line"><a:avLst/></a:prstGeom><a:ln><a:solidFill><a:srgbClr val="FF6600"/></a:solidFill></a:ln></p:spPr></p:cxnSp>
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
