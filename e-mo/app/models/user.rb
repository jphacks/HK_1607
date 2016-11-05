class User < ApplicationRecord
  validates :login_id, presence: true, length: { maximum: 20 }
  validates :password, presence: true, length: { maximum: 255 }
  validates :student_name, length: { maximum: 40 }
end
