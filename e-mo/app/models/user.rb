class User < ApplicationRecord
  # ログインIDの入力必須化, 最大文字数及び最小文字数の設定
  validates :login_id, uniqueness: true, presence: true, length: { in: 4..20 }
  # パスワードの入力必須化, 最大文字数及び最小文字数の設定
  validates :password, presence: true, length: { in: 4..127 }
  # 生徒名の最大文字数の設定
  validates :student_name, length: { maximum: 40 }
end
