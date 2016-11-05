class User < ApplicationRecord
  # ログインIDの入力必須化, 最大文字数及び最小文字数の設定
  validates :login_id, presence: true, length: { maximum: 20, minimum: 4 }
  # パスワードの入力必須化, 最大文字数及び最小文字数の設定
  validates :password, presence: true, length: { maximum: 255, minimum: 8 }
  # 生徒名の最大文字数の設定
  validates :student_name, length: { maximum: 40 }
end
