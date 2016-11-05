module ApplicationHelper

  # titleタグを返す
  def full_title(page_title = "")
    # 基本タイトル( | の右側 )の設定
    base_title = "e-mo"
    # 受け取るページタイトルがなかった場合基本タイトルをそのまま返す
    return base_title if page_title.empty?
    # 受け取ったページタイトルと基本タイトルを文字連結して返す
    return page_title + " | " + base_title
  end
end
